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
    public sealed class ReviseAction : TransitionLaneBase
    {
        public ReviseAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Revise;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessRevised;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_REVISE_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceRevise(Context, ref result);
            // 保留原 Service 语义：Factory 已 Success 则直接返回（不跑 Gate/Execute）
            if (result.Status == WfExecutedStatus.Success)
                return false;
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;
            ctx = ToContext(runtime);
            return true;
        }

        protected override void ExecuteCore(TransitionActionContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.reviseprocess.error", error);
        }
    }
}
