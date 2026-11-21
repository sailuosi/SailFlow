using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Logging;

namespace sailflow.engine.Hooks
{
    /// <summary>Gate / Hook / Notifier 分发与 Outbox 写入。</summary>
    public sealed class FlowCapabilityDispatcher
    {
        private const string ProcessCodeCachePrefix = "wf:processCode:";

        public FlowCapabilityDispatcher(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Registry = client.Capabilities;
        }

        public SailClient Sail { get; }
        public FlowCapabilityRegistry Registry { get; }

        /// <summary>由流程定义 Id+Version 解析 <see cref="ProcessEntity.ProcessCode"/> 并缓存。</summary>
        public string ResolveProcessCode(SQLBuilder kit, string processId, string version)
        {
            if (string.IsNullOrEmpty(processId))
                return null;

            version ??= string.Empty;
            var cacheKey = ProcessCodeCachePrefix + processId + ":" + version;
            var cached = Sail.Cache.Get<string>(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return cached;

            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetByVersion(kit, processId, version, throwException: false);
            var code = entity?.ProcessCode;
            if (!string.IsNullOrEmpty(code))
                Sail.Cache.Add(cacheKey, code, (int)TimeSpan.FromHours(1).TotalSeconds);
            return code;
        }

        /// <summary>Run 前 Gate；首个 Reject 即返回。</summary>
        public HookResult RunGates(SQLBuilder kit, FlowHookContext ctx)
        {
            if (ctx == null || Registry.Gates.Count == 0)
                return HookResult.Ok();

            EnsureProcessCode(kit, ctx);
            var log = Sail.Log;

            foreach (var gate in Registry.Gates)
            {
                if (!MatchesGate(gate, ctx))
                    continue;

                try
                {
                    var result = gate.Handler.Validate(ctx);
                    if (result != null && !result.IsOk)
                    {
                        Sail.CapabilityMetrics?.IncrementGateReject();
                        log?.Warning(
                            SailLogCategories.Gate,
                            "REJECT name={0} code={1} msg={2} pi={3}",
                            gate.Name,
                            result.Code ?? "-",
                            result.Message ?? "-",
                            ctx.ProcessInstanceId ?? "-");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(
                        SailLogCategories.Gate,
                        ex,
                        "FAIL name={0} pi={1}",
                        gate.Name,
                        ctx.ProcessInstanceId ?? "-");
                    throw;
                }
            }

            return HookResult.Ok();
        }

        /// <summary>
        /// Sync Hook，FailFast。始终同线程执行（有 kit 时连接不可跨线程）。
        /// 软超时在 <c>Handle</c> 返回后判定；进度日志 BEGIN/END 经 <see cref="SailClient.Log"/>。
        /// </summary>
        public void RunSyncHooks(SQLBuilder kit, FlowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (ctx == null || Registry.Hooks.Count == 0)
                return;

            ctx.EventType = eventType;
            EnsureProcessCode(kit, ctx);

            // 同事务 kit 挂到上下文，供 Sync Hook 读写未提交实体
            ctx.Kit = kit;

            var log = Sail.Log;
            var warnMs = Sail.CapabilityOptions?.SyncHookWarnMs ?? 0;
            var kitPresent = kit != null;

            foreach (var hook in Registry.Hooks.Where(h => h.Mode == HookMode.Sync))
            {
                if (!MatchesHook(hook, ctx, eventType))
                    continue;

                var timeoutMs = ResolveSyncHookTimeoutMs(hook);
                log?.Debug(
                    SailLogCategories.SyncHook,
                    "BEGIN name={0} event={1} kit={2} timeoutMs={3} pi={4} task={5}",
                    hook.Name,
                    eventType,
                    kitPresent,
                    timeoutMs,
                    ctx.ProcessInstanceId ?? "-",
                    ctx.TaskId ?? "-");

                var sw = Stopwatch.StartNew();
                try
                {
                    hook.Handler.Handle(ctx);
                    sw.Stop();
                    var elapsed = sw.ElapsedMilliseconds;

                    if (timeoutMs > 0 && elapsed > timeoutMs)
                    {
                        Sail.CapabilityMetrics?.IncrementSyncHookTimeout();
                        log?.Error(
                            SailLogCategories.SyncHook,
                            string.Format(
                                "FAIL name={0} event={1} elapsedMs={2} reason=timeout timeoutMs={3}",
                                hook.Name,
                                eventType,
                                elapsed,
                                timeoutMs));
                        throw new FlowSyncHookTimeoutException(
                            hook.Name, eventType, timeoutMs, elapsed, kitPresent);
                    }

                    if (warnMs > 0 && elapsed > warnMs)
                    {
                        log?.Warning(
                            SailLogCategories.SyncHook,
                            "SLOW name={0} event={1} elapsedMs={2} warnMs={3}",
                            hook.Name,
                            eventType,
                            elapsed,
                            warnMs);
                    }

                    log?.Debug(
                        SailLogCategories.SyncHook,
                        "END name={0} event={1} elapsedMs={2}",
                        hook.Name,
                        eventType,
                        elapsed);
                }
                catch (FlowSyncHookTimeoutException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    log?.Error(
                        SailLogCategories.SyncHook,
                        ex,
                        "FAIL name={0} event={1} elapsedMs={2}",
                        hook.Name,
                        eventType,
                        sw.ElapsedMilliseconds);
                    throw new FlowSyncHookException(hook.Name, eventType, ex);
                }

                Sail.CapabilityMetrics?.IncrementSyncHook();
            }
        }

        /// <summary>
        /// 解析 Sync 钩子超时：注册覆盖 → <see cref="FlowHookBase.TimeoutMs"/> → 全局回退。
        /// </summary>
        internal int ResolveSyncHookTimeoutMs(HookRegistration hook)
        {
            if (hook?.TimeoutMs is int regMs)
                return regMs;
            if (hook?.Handler is FlowHookBase bas)
                return bas.TimeoutMs;
            return Sail.CapabilityOptions?.SyncHookTimeoutMs ?? 0;
        }

        /// <summary>Async Hook 写入 Outbox（kit 非空时同事务）。</summary>
        public void EnqueueAsyncHooks(SQLBuilder kit, FlowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (ctx == null || Registry.Hooks.Count == 0)
                return;

            ctx.EventType = eventType;
            EnsureProcessCode(kit, ctx);

            var om = Sail.ManagerFactory.HookOutbox;
            var log = Sail.Log;
            foreach (var hook in Registry.Hooks.Where(h => h.Mode == HookMode.AsyncOutbox))
            {
                if (!MatchesHook(hook, ctx, eventType))
                    continue;

                // Kit 不可入 Outbox（自引用环）；临时摘掉再序列化
                var savedKit = ctx.Kit;
                ctx.Kit = null;
                string payload;
                try
                {
                    payload = Sail.JsonSerializer.Serialize(ctx);
                }
                catch (Exception ex)
                {
                    log?.Error(
                        SailLogCategories.AsyncHook,
                        ex,
                        "ENQUEUE_FAIL name={0} event={1} pi={2}",
                        hook.Name,
                        eventType,
                        ctx.ProcessInstanceId ?? "-");
                    throw;
                }
                finally
                {
                    ctx.Kit = savedKit;
                }

                var row = new HookOutboxEntity
                {
                    Kind = HookOutboxKinds.Hook,
                    HandlerName = hook.Name,
                    EventType = eventType.ToString(),
                    ProcessId = ctx.ProcessId,
                    ProcessCode = ctx.ProcessCode,
                    ProcessInstanceId = ctx.ProcessInstanceId,
                    ActivityInstanceId = ctx.ActivityInstanceId,
                    TaskId = ctx.TaskId,
                    PayloadJson = payload,
                };

                if (kit != null)
                    om.InsertPending(kit, row);
                else
                    om.InsertPending(Sail.UseRunSQL(), row);

                Sail.CapabilityMetrics?.IncrementOutboxEnqueue();
                log?.Message(
                    SailLogCategories.AsyncHook,
                    "ENQUEUE name={0} event={1} pi={2}",
                    hook.Name,
                    eventType,
                    ctx.ProcessInstanceId ?? "-");
            }
        }

        /// <summary>待办 Notifier 默认 AsyncOutbox。</summary>
        public void EnqueueTaskNotifiers(SQLBuilder kit, TaskNotifyContext ctx)
        {
            if (ctx == null || Registry.Notifiers.Count == 0)
                return;

            EnsureProcessCode(kit, ctx);

            var om = Sail.ManagerFactory.HookOutbox;
            foreach (var notifier in Registry.Notifiers)
            {
                if (!MatchesNotifier(notifier, ctx))
                    continue;

                var payload = Sail.JsonSerializer.Serialize(ctx);
                var row = new HookOutboxEntity
                {
                    Kind = HookOutboxKinds.Notify,
                    HandlerName = notifier.Name,
                    EventType = ctx.Kind.ToString(),
                    ProcessId = ctx.ProcessId,
                    ProcessCode = ctx.ProcessCode,
                    ProcessInstanceId = ctx.ProcessInstanceId,
                    ActivityInstanceId = ctx.ActivityInstanceId,
                    TaskId = ctx.TaskId,
                    PayloadJson = payload,
                };

                if (kit != null)
                    om.InsertPending(kit, row);
                else
                    om.InsertPending(Sail.UseRunSQL(), row);

                Sail.CapabilityMetrics?.IncrementOutboxEnqueue();
            }
        }

        /// <summary>待办创建/完成：Sync/Async Hook + Notifier Outbox。</summary>
        public void FireTaskLifecycle(
            SQLBuilder kit,
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            FlowCommandContext runner,
            TaskNotifyKind kind)
        {
            if (task == null)
                return;

            // runner 保留签名兼容调用方；上下文由 task/activityInstance 构造
            _ = runner;

            // Created/Urged 有独立事件；其余（Completed/Cancelled/Entrusted）保持历史映射 OnTaskCompleted
            var eventType = kind switch
            {
                TaskNotifyKind.Created => EventFireTypeEnum.OnTaskCreated,
                TaskNotifyKind.Urged => EventFireTypeEnum.OnTaskUrged,
                _ => EventFireTypeEnum.OnTaskCompleted
            };
            var operation = kind == TaskNotifyKind.Urged ? FlowOperation.Urge : FlowOperation.Run;

            var hookCtx = BuildHookContextFromTask(task, activityInstance, eventType, operation);
            RunSyncHooks(kit, hookCtx, eventType);
            EnqueueAsyncHooks(kit, hookCtx, eventType);

            var notifyCtx = BuildNotifyContext(task, activityInstance, kind);
            EnqueueTaskNotifiers(kit, notifyCtx);
        }

        /// <summary>抄送创建：可选 FlowHook（OnCopytoCreated）+ 独立 CopytoNotifier Outbox。</summary>
        public void FireCopytoLifecycle(SQLBuilder kit, CopytoNotifyContext ctx)
        {
            if (ctx == null)
                return;

            EnsureProcessCode(kit, ctx);

            var eventType = ctx.Kind == CopytoNotifyKind.Created
                ? EventFireTypeEnum.OnCopytoCreated
                : EventFireTypeEnum.OnCopytoCreated;

            var hookCtx = new FlowHookContext
            {
                BizChannel = ctx.BizChannel,
                BizPK = ctx.BizPK,
                ProcessId = ctx.ProcessId,
                ProcessCode = ctx.ProcessCode,
                Version = ctx.Version,
                ProcessInstanceId = ctx.ProcessInstanceId,
                TaskId = ctx.TaskId,
                SignId = ctx.UserId,
                SignName = ctx.UserName,
                EventType = eventType,
                Operation = FlowOperation.Run,
                Kit = kit
            };
            RunSyncHooks(kit, hookCtx, eventType);
            EnqueueAsyncHooks(kit, hookCtx, eventType);
            EnqueueCopytoNotifiers(kit, ctx);
        }

        /// <summary>抄送 Notifier 默认 AsyncOutbox（Kind=<see cref="HookOutboxKinds.CopytoNotify"/>）。</summary>
        public void EnqueueCopytoNotifiers(SQLBuilder kit, CopytoNotifyContext ctx)
        {
            if (ctx == null || Registry.CopytoNotifiers.Count == 0)
                return;

            EnsureProcessCode(kit, ctx);

            var om = Sail.ManagerFactory.HookOutbox;
            foreach (var notifier in Registry.CopytoNotifiers)
            {
                if (!MatchesCopytoNotifier(notifier, ctx))
                    continue;

                var payload = Sail.JsonSerializer.Serialize(ctx);
                var row = new HookOutboxEntity
                {
                    Kind = HookOutboxKinds.CopytoNotify,
                    HandlerName = notifier.Name,
                    EventType = ctx.Kind.ToString(),
                    ProcessId = ctx.ProcessId,
                    ProcessCode = ctx.ProcessCode,
                    ProcessInstanceId = ctx.ProcessInstanceId,
                    TaskId = ctx.TaskId,
                    PayloadJson = payload,
                };

                if (kit != null)
                    om.InsertPending(kit, row);
                else
                    om.InsertPending(Sail.UseRunSQL(), row);

                Sail.CapabilityMetrics?.IncrementOutboxEnqueue();
            }
        }

        /// <summary>认领并消费 Pending Outbox（Worker / 测试 drain）。</summary>
        public int DispatchPendingOutbox(
            SQLBuilder kit,
            int limit = 50,
            int maxAttempts = HookOutboxOptions.DefaultMaxAttempts,
            CancellationToken cancellationToken = default)
        {
            var om = Sail.ManagerFactory.HookOutbox;
            var claimed = om.TryClaimPending(kit, limit, maxAttempts);
            return DispatchClaimedOutbox(claimed, maxAttempts, cancellationToken);
        }

        /// <summary>处理已认领行（每条独立短事务）。</summary>
        public int DispatchClaimedOutbox(
            IList<HookOutboxEntity> rows,
            int maxAttempts = HookOutboxOptions.DefaultMaxAttempts,
            CancellationToken cancellationToken = default) =>
            DispatchClaimedOutboxAsync(rows, maxAttempts, cancellationToken).GetAwaiter().GetResult();

        public async Task<int> DispatchClaimedOutboxAsync(
            IList<HookOutboxEntity> rows,
            int maxAttempts = HookOutboxOptions.DefaultMaxAttempts,
            CancellationToken cancellationToken = default)
        {
            if (rows == null || rows.Count == 0)
                return 0;

            var om = Sail.ManagerFactory.HookOutbox;
            var processed = 0;
            // 配置了 InProcessThrottle 的 Notify：并行入车道，由 MaxDegree 限并发；其余仍串行
            List<Task<bool>> throttledNotifyTasks = null;

            foreach (var row in rows)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (row.Kind == HookOutboxKinds.Notify && NotifierHasInProcessThrottle(row.HandlerName))
                {
                    throttledNotifyTasks ??= new List<Task<bool>>();
                    throttledNotifyTasks.Add(ProcessOneOutboxRowAsync(om, row, maxAttempts, cancellationToken));
                    continue;
                }

                if (await ProcessOneOutboxRowAsync(om, row, maxAttempts, cancellationToken).ConfigureAwait(false))
                    processed++;
            }

            if (throttledNotifyTasks is { Count: > 0 })
            {
                var results = await Task.WhenAll(throttledNotifyTasks).ConfigureAwait(false);
                foreach (var ok in results)
                {
                    if (ok)
                        processed++;
                }
            }

            return processed;
        }

        private async Task<bool> ProcessOneOutboxRowAsync(
            HookOutboxManager om,
            HookOutboxEntity row,
            int maxAttempts,
            CancellationToken cancellationToken)
        {
            try
            {
                await DispatchOutboxRowAsync(row, cancellationToken).ConfigureAwait(false);

                using (var kit = Sail.UseRunSQL())
                {
                    kit.beginTransaction();
                    om.MarkSucceeded(kit, row);
                    kit.commit();
                }

                Sail.CapabilityMetrics?.IncrementOutboxSucceeded();
                return true;
            }
            catch (Exception ex)
            {
                using (var kit = Sail.UseRunSQL())
                {
                    kit.beginTransaction();
                    om.MarkFailed(kit, row, ex.Message, maxAttempts);
                    kit.commit();
                }

                Sail.CapabilityMetrics?.IncrementOutboxFailed();
                if (row.Status == HookOutboxStatus.Dead)
                {
                    Sail.CapabilityMetrics?.IncrementOutboxDead();
                    Trace.TraceWarning(
                        $"[sailflow] Hook outbox dead-letter id={row.TaskId} kind={row.Kind} handler={row.HandlerName}: {ex.Message}");
                }

                return false;
            }
        }

        private bool NotifierHasInProcessThrottle(string handlerName)
        {
            if (string.IsNullOrEmpty(handlerName))
                return false;

            var notifier = Registry.Notifiers.FirstOrDefault(n =>
                string.Equals(n.Name, handlerName, StringComparison.OrdinalIgnoreCase));
            return notifier?.InProcessThrottle != null;
        }

        private async Task DispatchOutboxRowAsync(HookOutboxEntity row, CancellationToken cancellationToken)
        {
            if (row.Kind == HookOutboxKinds.Hook)
                DispatchHookOutboxRow(row);
            else if (row.Kind == HookOutboxKinds.Notify)
                await DispatchNotifyOutboxRow(row, cancellationToken).ConfigureAwait(false);
            else if (row.Kind == HookOutboxKinds.CopytoNotify)
                await DispatchCopytoNotifyOutboxRow(row, cancellationToken).ConfigureAwait(false);
            else
                throw new InvalidOperationException($"Unknown outbox kind '{row.Kind}'.");
        }

        /// <summary>已注册 Notifier 同步调用（失败隔离）。</summary>
        public async Task DispatchNotifiersAsync(TaskNotifyContext ctx, CancellationToken cancellationToken = default)
        {
            if (ctx == null)
                return;

            EnsureProcessCode(null, ctx);

            foreach (var notifier in Registry.Notifiers)
            {
                if (!MatchesNotifier(notifier, ctx))
                    continue;

                try
                {
                    await notifier.Handler.NotifyAsync(ctx, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Notifier 失败隔离：不阻断其它通道
                }
            }
        }

        public FlowHookContext BuildHookContextFromRuntime(
            FlowCommandContext runner,
            string processInstanceId,
            ActivityInstanceEntity activityInstance,
            string taskId,
            EventFireTypeEnum eventType,
            FlowOperation operation)
        {
            return new FlowHookContext
            {
                BizChannel = runner?.BizChannel,
                BizPK = runner?.BizPK,
                ProcessId = runner?.ProcessId ?? activityInstance?.ProcessId,
                Version = runner?.Version,
                ProcessInstanceId = processInstanceId,
                ActivityId = activityInstance?.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = activityInstance?.ActivityInstanceId,
                TaskId = taskId,
                SignId = runner?.UserId,
                SignName = runner?.UserName,
                EventType = eventType,
                Operation = operation,
            };
        }

        private void DispatchHookOutboxRow(HookOutboxEntity row)
        {
            var hook = Registry.Hooks.FirstOrDefault(h =>
                string.Equals(h.Name, row.HandlerName, StringComparison.OrdinalIgnoreCase));
            if (hook?.Handler == null)
                throw new InvalidOperationException($"Hook handler '{row.HandlerName}' not registered.");

            var ctx = Sail.JsonSerializer.Deserialize<FlowHookContext>(row.PayloadJson);
            if (ctx == null)
                throw new InvalidOperationException("Hook outbox payload is empty.");

            hook.Handler.Handle(ctx);
        }

        private async Task DispatchCopytoNotifyOutboxRow(HookOutboxEntity row, CancellationToken cancellationToken)
        {
            var notifier = Registry.CopytoNotifiers.FirstOrDefault(n =>
                string.Equals(n.Name, row.HandlerName, StringComparison.OrdinalIgnoreCase));
            if (notifier?.Handler == null)
                throw new InvalidOperationException($"CopytoNotifier '{row.HandlerName}' not registered.");

            var ctx = Sail.JsonSerializer.Deserialize<CopytoNotifyContext>(row.PayloadJson);
            if (ctx == null)
                throw new InvalidOperationException("CopytoNotify outbox payload is empty.");

            await notifier.Handler.NotifyAsync(ctx, cancellationToken).ConfigureAwait(false);
            Sail.CapabilityMetrics?.IncrementNotifierDispatch();
        }

        private async Task DispatchNotifyOutboxRow(HookOutboxEntity row, CancellationToken cancellationToken)
        {
            var notifier = Registry.Notifiers.FirstOrDefault(n =>
                string.Equals(n.Name, row.HandlerName, StringComparison.OrdinalIgnoreCase));
            if (notifier?.Handler == null)
                throw new InvalidOperationException($"Notifier '{row.HandlerName}' not registered.");

            var ctx = Sail.JsonSerializer.Deserialize<TaskNotifyContext>(row.PayloadJson);
            if (ctx == null)
                throw new InvalidOperationException("Notify outbox payload is empty.");

            // InProcessThrottle：认领后入有界 Channel，等真实 NotifyAsync 完成再返回 → 外层才 MarkSucceeded
            if (notifier.InProcessThrottle != null)
            {
                await Sail.NotifierThrottleHub
                    .RunAsync(
                        notifier.Name,
                        notifier.InProcessThrottle,
                        ct => notifier.Handler.NotifyAsync(ctx, ct),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await notifier.Handler.NotifyAsync(ctx, cancellationToken).ConfigureAwait(false);
            }

            Sail.CapabilityMetrics?.IncrementNotifierDispatch();
        }

        private static FlowHookContext BuildHookContextFromTask(
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            EventFireTypeEnum eventType,
            FlowOperation operation) =>
            new()
            {
                BizChannel = task.BizChannel,
                BizPK = task.BizPK,
                ProcessId = task.ProcessId,
                ProcessInstanceId = task.ProcessInstanceId,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId,
                TaskId = task.TaskId,
                SignId = task.SignId,
                SignName = task.SignName,
                EventType = eventType,
                Operation = operation,
            };

        private static TaskNotifyContext BuildNotifyContext(
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            TaskNotifyKind kind) =>
            new()
            {
                BizChannel = task.BizChannel,
                BizPK = task.BizPK,
                ProcessId = task.ProcessId,
                ProcessInstanceId = task.ProcessInstanceId,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId,
                TaskId = task.TaskId,
                SignId = task.SignId,
                SignName = task.SignName,
                Kind = kind,
            };

        private void EnsureProcessCode(SQLBuilder kit, FlowHookContext ctx)
        {
            if (ctx == null || !string.IsNullOrEmpty(ctx.ProcessCode))
                return;
            ctx.ProcessCode = ResolveProcessCode(kit ?? Sail.UseRunSQL(), ctx.ProcessId, ctx.Version);
        }

        private void EnsureProcessCode(SQLBuilder kit, TaskNotifyContext ctx)
        {
            if (ctx == null || !string.IsNullOrEmpty(ctx.ProcessCode))
                return;
            ctx.ProcessCode = ResolveProcessCode(kit ?? Sail.UseRunSQL(), ctx.ProcessId, ctx.Version);
        }

        private void EnsureProcessCode(SQLBuilder kit, CopytoNotifyContext ctx)
        {
            if (ctx == null || !string.IsNullOrEmpty(ctx.ProcessCode))
                return;
            ctx.ProcessCode = ResolveProcessCode(kit ?? Sail.UseRunSQL(), ctx.ProcessId, ctx.Version);
        }

        private static bool MatchesGate(GateRegistration gate, FlowHookContext ctx) =>
            FlowCapabilityMatch.MatchesProcess(gate.Match, gate.ProcessCodes, gate.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
            && FlowCapabilityMatch.MatchesActivity(gate.ActivityCodes, ctx.ActivityCode);

        private static bool MatchesHook(HookRegistration hook, FlowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (hook.Events == null || hook.Events.Length == 0 || !hook.Events.Contains(eventType))
                return false;
            return FlowCapabilityMatch.MatchesProcess(hook.Match, hook.ProcessCodes, hook.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
                   && FlowCapabilityMatch.MatchesActivity(hook.ActivityCodes, ctx.ActivityCode);
        }

        private static bool MatchesNotifier(NotifierRegistration notifier, TaskNotifyContext ctx)
        {
            if (notifier.TaskKinds == null || notifier.TaskKinds.Length == 0 || !notifier.TaskKinds.Contains(ctx.Kind))
                return false;
            return FlowCapabilityMatch.MatchesProcess(notifier.Match, notifier.ProcessCodes, notifier.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
                   && FlowCapabilityMatch.MatchesActivity(notifier.ActivityCodes, ctx.ActivityCode);
        }

        private static bool MatchesCopytoNotifier(CopytoNotifierRegistration notifier, CopytoNotifyContext ctx)
        {
            if (notifier.CopytoKinds == null || notifier.CopytoKinds.Length == 0
                || !notifier.CopytoKinds.Contains(ctx.Kind))
                return false;
            return FlowCapabilityMatch.MatchesProcess(
                notifier.Match, notifier.ProcessCodes, notifier.ProcessIds, ctx.ProcessCode, ctx.ProcessId);
        }
    }
}
