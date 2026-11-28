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
    public sealed class ReverseLane : TransitionLaneBase
    {
        public ReverseLane(SailClient sail, SQLBuilder kit, FlowCommandContext context)
            : base(sail, kit, context)
        {
        }

        public override FlowOperation Operation => FlowOperation.Reverse;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessReversed;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_REVERSE_ERROR;

        protected override bool TryLoadContext(out TransitionLaneContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceReverse(Context, ref result);
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;
            ctx = ToContext(runtime);
            return true;
        }

        protected override void ExecuteCore(TransitionLaneContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.reverseprocess.error", error);
        }
    }
}
