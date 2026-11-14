using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Factories;
using sailflow.engine.Hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Transition
{
    /// <summary>撤回：CreateRuntimeInstanceWithdraw → Execute；族群固化 Gate/成功 Hook。</summary>
    public sealed class WithdrawAction : TransitionActionBase
    {
        public WithdrawAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Withdraw;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessWithdrawn;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_WITHDRAW_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            var runtime = SailFactoryAccess.Runtime(Sail).CreateRuntimeInstanceWithdraw(Runner, ref result);
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;

            ctx = new TransitionActionContext
            {
                ProcessInstanceId = runtime.ProcessInstanceId,
                RunningActivityInstance = runtime.RunningActivityInstance,
                TaskId = runtime.TaskView?.TaskId,
                Runtime = runtime
            };
            return true;
        }

        protected override void ExecuteCore(TransitionActionContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.withdrawprocess.error", error);
        }
    }
}
