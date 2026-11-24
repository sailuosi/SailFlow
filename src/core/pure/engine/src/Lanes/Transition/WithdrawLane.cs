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
    /// <summary>撤回：CreateRuntimeInstanceWithdraw → Execute；族群固化 Gate/成功 Hook。</summary>
    public sealed class WithdrawLane : TransitionLaneBase
    {
        public WithdrawLane(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Withdraw;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessWithdrawn;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_WITHDRAW_ERROR;

        protected override bool TryLoadContext(out TransitionLaneContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceWithdraw(Context, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.withdrawprocess.error", error);
        }
    }
}
