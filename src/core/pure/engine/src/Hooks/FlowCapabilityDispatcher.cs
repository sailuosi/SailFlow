using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Event;

namespace saiflow.engine.Hooks
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

            foreach (var gate in Registry.Gates)
            {
                if (!MatchesGate(gate, ctx))
                    continue;

                var result = gate.Handler.Validate(ctx);
                if (result != null && !result.IsOk)
                {
                    Sail.CapabilityMetrics?.IncrementGateReject();
                    return result;
                }
            }

            return HookResult.Ok();
        }

        /// <summary>Sync Hook，FailFast（异常向上抛）。</summary>
        public void RunSyncHooks(SQLBuilder kit, FlowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (ctx == null || Registry.Hooks.Count == 0)
                return;

            ctx.EventType = eventType;
            EnsureProcessCode(kit, ctx);

            var timeoutMs = Sail.CapabilityOptions?.SyncHookTimeoutMs ?? 0;

            foreach (var hook in Registry.Hooks.Where(h => h.Mode == HookMode.Sync))
            {
                if (!MatchesHook(hook, ctx, eventType))
                    continue;

                if (timeoutMs <= 0)
                {
                    hook.Handler.Handle(ctx);
                }
                else
                {
                    // Dedicated thread + Join: avoids sync-over-async deadlocks on ASP.NET sync contexts.
                    Exception handlerError = null;
                    var worker = new Thread(() =>
                    {
                        try
                        {
                            hook.Handler.Handle(ctx);
                        }
                        catch (Exception ex)
                        {
                            handlerError = ex;
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "saiflow-sync-hook:" + hook.Name,
                    };
                    worker.Start();
                    if (!worker.Join(timeoutMs))
                    {
                        Sail.CapabilityMetrics?.IncrementSyncHookTimeout();
                        throw new FlowSyncHookTimeoutException(hook.Name, timeoutMs);
                    }

                    if (handlerError != null)
                        throw handlerError;
                }

                Sail.CapabilityMetrics?.IncrementSyncHook();
            }
        }

        /// <summary>Async Hook 写入 Outbox（kit 非空时同事务）。</summary>
        public void EnqueueAsyncHooks(SQLBuilder kit, FlowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (ctx == null || Registry.Hooks.Count == 0)
                return;

            ctx.EventType = eventType;
            EnsureProcessCode(kit, ctx);

            var om = Sail.ManagerFactory.HookOutbox;
            foreach (var hook in Registry.Hooks.Where(h => h.Mode == HookMode.AsyncOutbox))
            {
                if (!MatchesHook(hook, ctx, eventType))
                    continue;

                var payload = Sail.JsonSerializer.Serialize(ctx);
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

            var eventType = kind == TaskNotifyKind.Created
                ? EventFireTypeEnum.OnTaskCreated
                : EventFireTypeEnum.OnTaskCompleted;

            var hookCtx = BuildHookContextFromTask(task, activityInstance, eventType, FlowOperation.Run);
            RunSyncHooks(kit, hookCtx, eventType);
            EnqueueAsyncHooks(kit, hookCtx, eventType);

            var notifyCtx = BuildNotifyContext(task, activityInstance, kind);
            EnqueueTaskNotifiers(kit, notifyCtx);
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
                        $"[saiflow] Hook outbox dead-letter id={row.Id} kind={row.Kind} handler={row.HandlerName}: {ex.Message}");
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
            int processInstanceId,
            ActivityInstanceEntity activityInstance,
            int? taskId,
            EventFireTypeEnum eventType,
            FlowOperation operation)
        {
            return new FlowHookContext
            {
                AppName = runner?.AppName,
                AppInstanceId = runner?.AppInstanceId,
                ProcessId = runner?.ProcessId ?? activityInstance?.ProcessId,
                Version = runner?.Version,
                ProcessInstanceId = processInstanceId,
                ActivityId = activityInstance?.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = activityInstance?.Id ?? 0,
                TaskId = taskId,
                AssignedUserId = runner?.UserId,
                AssignedUserName = runner?.UserName,
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
                AppName = task.AppName,
                AppInstanceId = task.AppInstanceId,
                ProcessId = task.ProcessId,
                ProcessInstanceId = task.ProcessInstanceId.Value,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId.Value,
                TaskId = task.Id,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = task.AssignedUserName,
                EventType = eventType,
                Operation = operation,
            };

        private static TaskNotifyContext BuildNotifyContext(
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            TaskNotifyKind kind) =>
            new()
            {
                AppName = task.AppName,
                AppInstanceId = task.AppInstanceId,
                ProcessId = task.ProcessId,
                ProcessInstanceId = task.ProcessInstanceId.Value,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId.Value,
                TaskId = task.Id.Value,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = task.AssignedUserName,
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
    }
}
