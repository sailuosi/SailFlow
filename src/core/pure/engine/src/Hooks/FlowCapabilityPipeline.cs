using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Client;
using sailflow.engine.Result;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>Gate / 成功 Hook 的公共编排（避免 WorkflowService 重复）。</summary>
    public static class FlowCapabilityPipeline
    {
        public static FlowHookContext BuildGateContext(
            SailClient sail,
            SQLBuilder kit,
            FlowCommandContext runner,
            string processInstanceId,
            ActivityInstanceEntity activityInstance,
            string taskId,
            FlowOperation operation) =>
            BuildHookContext(
                sail,
                kit,
                runner,
                processInstanceId,
                activityInstance,
                taskId,
                EventFireTypeEnum.OnValidating,
                operation);

        public static FlowHookContext BuildHookContext(
            SailClient sail,
            SQLBuilder kit,
            FlowCommandContext runner,
            string processInstanceId,
            ActivityInstanceEntity activityInstance,
            string taskId,
            EventFireTypeEnum eventType,
            FlowOperation operation)
        {
            var dispatcher = sail.CapabilityDispatcher;
            var ctx = dispatcher.BuildHookContextFromRuntime(
                runner,
                processInstanceId,
                activityInstance,
                taskId,
                eventType,
                operation);

            ctx.Version = runner?.Version;
            ctx.ProcessCode = runner?.ProcessCode ?? ctx.ProcessCode;

            if (kit != null && !string.IsNullOrWhiteSpace(processInstanceId))
            {
                if (string.IsNullOrEmpty(ctx.Version))
                {
                    var processInstance = sail.ProcessInstances.GetById(kit, processInstanceId);
                    ctx.Version = processInstance?.Version ?? ctx.Version;
                }
            }

            if (kit != null && string.IsNullOrEmpty(ctx.ProcessCode) && !string.IsNullOrEmpty(ctx.ProcessId))
                ctx.ProcessCode = dispatcher.ResolveProcessCode(kit, ctx.ProcessId, ctx.Version);

            return ctx;
        }

        /// <summary>RunGates；Reject 时已写入 <paramref name="result"/>。返回 false 表示应中止 Execute。</summary>
        public static bool TryGate(SailClient sail, SQLBuilder kit, FlowHookContext ctx, WfExecutedResult result)
        {
            if (sail.Capabilities.Gates.Count == 0)
                return true;

            var gateResult = sail.CapabilityDispatcher.RunGates(kit, ctx);
            if (gateResult != null && !gateResult.IsOk)
            {
                result.ApplyGateReject(gateResult);
                return false;
            }

            return true;
        }

        public static void AfterSuccess(
            SailClient sail,
            SQLBuilder kit,
            FlowHookContext ctx,
            EventFireTypeEnum successEvent)
        {
            var dispatcher = sail.CapabilityDispatcher;
            dispatcher.RunSyncHooks(kit, ctx, successEvent);
            dispatcher.EnqueueAsyncHooks(kit, ctx, successEvent);
        }
    }
}
