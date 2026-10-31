using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using Newtonsoft.Json;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Event;

namespace saiflow.engine.Hooks
{
    /// <summary>Gate / Hook / Notifier 分发与 Outbox 写入。</summary>
    public sealed class WorkflowCapabilityDispatcher
    {
        private const string ProcessCodeCachePrefix = "wf:processCode:";

        public WorkflowCapabilityDispatcher(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Registry = client.Capabilities;
        }

        public SailClient Sail { get; }
        public WorkflowCapabilityRegistry Registry { get; }

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

            var pm = new ProcessManager(Sail);
            var entity = pm.GetByVersion(kit, processId, version, throwException: false);
            var code = entity?.ProcessCode;
            if (!string.IsNullOrEmpty(code))
                Sail.Cache.Add(cacheKey, code, (int)TimeSpan.FromHours(1).TotalSeconds);
            return code;
        }

        /// <summary>Run 前 Gate；首个 Reject 即返回。</summary>
        public HookResult RunGates(SQLBuilder kit, WorkflowHookContext ctx)
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
        public void RunSyncHooks(SQLBuilder kit, WorkflowHookContext ctx, EventFireTypeEnum eventType)
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
                        throw new WorkflowSyncHookTimeoutException(hook.Name, timeoutMs);
                    }

                    if (handlerError != null)
                        throw handlerError;
                }

                Sail.CapabilityMetrics?.IncrementSyncHook();
            }
        }

        /// <summary>Async Hook 写入 Outbox（kit 非空时同事务）。</summary>
        public void EnqueueAsyncHooks(SQLBuilder kit, WorkflowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (ctx == null || Registry.Hooks.Count == 0)
                return;

            ctx.EventType = eventType;
            EnsureProcessCode(kit, ctx);

            var om = new HookOutboxManager(Sail);
            foreach (var hook in Registry.Hooks.Where(h => h.Mode == HookMode.AsyncOutbox))
            {
                if (!MatchesHook(hook, ctx, eventType))
                    continue;

                var payload = JsonConvert.SerializeObject(ctx);
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

            var om = new HookOutboxManager(Sail);
            foreach (var notifier in Registry.Notifiers)
            {
                if (!MatchesNotifier(notifier, ctx))
                    continue;

                var payload = JsonConvert.SerializeObject(ctx);
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

        /// <summary>待办创建/完成：Sync Hook + Subscribe 事件 + Notifier Outbox。</summary>
        public void FireTaskLifecycle(
            SQLBuilder kit,
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            WfAppRunner runner,
            TaskNotifyKind kind)
        {
            if (task == null)
                return;

            var eventType = kind == TaskNotifyKind.Created
                ? EventFireTypeEnum.OnTaskCreated
                : EventFireTypeEnum.OnTaskCompleted;

            var hookCtx = BuildHookContextFromTask(task, activityInstance, eventType, WorkflowOperation.Run);
            RunSyncHooks(kit, hookCtx, eventType);
            EnqueueAsyncHooks(kit, hookCtx, eventType);

            var notifyCtx = BuildNotifyContext(task, activityInstance, kind);
            EnqueueTaskNotifiers(kit, notifyCtx);

            if (runner?.EventSubscriptionList != null && runner.EventSubscriptionList.Count > 0)
            {
                var eventContext = new EventContext
                {
                    AppInstanceId = task.AppInstanceId,
                    ProcessId = task.ProcessId,
                    ProcessInstanceId = task.ProcessInstanceId,
                    ActivityId = task.ActivityId,
                    ActivityName = task.ActivityName,
                    ActivityCode = activityInstance?.ActivityCode,
                };
                EventExecutor.InvokeExternalEvent(Sail, kit, eventType, runner.EventSubscriptionList, eventContext);
            }
        }

        /// <summary>认领并消费 Pending Outbox（Worker / 测试 drain）。</summary>
        public int DispatchPendingOutbox(
            SQLBuilder kit,
            int limit = 50,
            int maxAttempts = HookOutboxOptions.DefaultMaxAttempts,
            CancellationToken cancellationToken = default)
        {
            var om = new HookOutboxManager(Sail);
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

            var om = new HookOutboxManager(Sail);
            var processed = 0;

            foreach (var row in rows)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

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
                    processed++;
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
                }
            }

            return processed;
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

        public WorkflowHookContext BuildHookContextFromRuntime(
            WfAppRunner runner,
            int processInstanceId,
            ActivityInstanceEntity activityInstance,
            int? taskId,
            EventFireTypeEnum eventType,
            WorkflowOperation operation)
        {
            return new WorkflowHookContext
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

            var ctx = JsonConvert.DeserializeObject<WorkflowHookContext>(row.PayloadJson);
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

            var ctx = JsonConvert.DeserializeObject<TaskNotifyContext>(row.PayloadJson);
            if (ctx == null)
                throw new InvalidOperationException("Notify outbox payload is empty.");

            await notifier.Handler.NotifyAsync(ctx, cancellationToken).ConfigureAwait(false);
            Sail.CapabilityMetrics?.IncrementNotifierDispatch();
        }

        private static WorkflowHookContext BuildHookContextFromTask(
            TaskEntity task,
            ActivityInstanceEntity activityInstance,
            EventFireTypeEnum eventType,
            WorkflowOperation operation) =>
            new()
            {
                AppName = task.AppName,
                AppInstanceId = task.AppInstanceId,
                ProcessId = task.ProcessId,
                ProcessInstanceId = task.ProcessInstanceId,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId,
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
                ProcessInstanceId = task.ProcessInstanceId,
                ActivityId = task.ActivityId,
                ActivityCode = activityInstance?.ActivityCode,
                ActivityInstanceId = task.ActivityInstanceId,
                TaskId = task.Id,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = task.AssignedUserName,
                Kind = kind,
            };

        private void EnsureProcessCode(SQLBuilder kit, WorkflowHookContext ctx)
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

        private static bool MatchesGate(GateRegistration gate, WorkflowHookContext ctx) =>
            WorkflowCapabilityMatch.MatchesProcess(gate.Match, gate.ProcessCodes, gate.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
            && WorkflowCapabilityMatch.MatchesActivity(gate.ActivityCodes, ctx.ActivityCode);

        private static bool MatchesHook(HookRegistration hook, WorkflowHookContext ctx, EventFireTypeEnum eventType)
        {
            if (hook.Events == null || hook.Events.Length == 0 || !hook.Events.Contains(eventType))
                return false;
            return WorkflowCapabilityMatch.MatchesProcess(hook.Match, hook.ProcessCodes, hook.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
                   && WorkflowCapabilityMatch.MatchesActivity(hook.ActivityCodes, ctx.ActivityCode);
        }

        private static bool MatchesNotifier(NotifierRegistration notifier, TaskNotifyContext ctx)
        {
            if (notifier.TaskKinds == null || notifier.TaskKinds.Length == 0 || !notifier.TaskKinds.Contains(ctx.Kind))
                return false;
            return WorkflowCapabilityMatch.MatchesProcess(notifier.Match, notifier.ProcessCodes, notifier.ProcessIds, ctx.ProcessCode, ctx.ProcessId)
                   && WorkflowCapabilityMatch.MatchesActivity(notifier.ActivityCodes, ctx.ActivityCode);
        }
    }
}
