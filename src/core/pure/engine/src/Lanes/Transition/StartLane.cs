using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.factory;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>
    /// 启动：CreateRuntimeInstanceStartup → Gate（可补 Start 活动码）→ Execute → OnProcessStarted。
    /// Gate 时尚未落库 PI，Hook 用 <see cref="WfExecutedResult.ProcessInstanceIdStarted"/>。
    /// </summary>
    public sealed class StartLane : TransitionLaneBase
    {
        public StartLane(SailClient sail, SQLBuilder kit, FlowCommandContext context)
            : base(sail, kit, context)
        {
        }

        public override FlowOperation Operation => FlowOperation.Start;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessStarted;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_START_ERROR;

        protected override bool TryLoadContext(out TransitionLaneContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceStartup(Context, Kit, ref result);
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;
            ctx = ToContext(runtime);
            return true;
        }

        protected override void EnrichGateContext(FlowHookContext gateCtx, TransitionLaneContext ctx)
        {
            var startActivity = ctx.Runtime?.ProcessModel?.GetStartActivity();
            if (startActivity == null)
                return;
            gateCtx.ActivityId = startActivity.ActivityId;
            gateCtx.ActivityCode = startActivity.ActivityCode;
        }

        protected override string ResolveHookProcessInstanceId(TransitionLaneContext ctx, WfExecutedResult result)
            => !string.IsNullOrWhiteSpace(result.ProcessInstanceIdStarted)
                ? result.ProcessInstanceIdStarted
                : ctx.ProcessInstanceId;

        protected override void ExecuteCore(TransitionLaneContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.startprocess.error", error);
        }
    }
}
