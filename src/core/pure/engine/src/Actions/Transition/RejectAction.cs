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
    /// <summary>驳回：填 Startup 办理人 → CreateRuntimeInstanceReject → Execute。</summary>
    public sealed class RejectAction : TransitionActionBase
    {
        public RejectAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Reject;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessRejected;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_REJECT_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            Runner.NextActivityPerformers = TransitionNextPerformers.Fill(Sail, Runner, JumpOptionEnum.Startup);
            var runtime = SailFactoryAccess.Runtime(Sail).CreateRuntimeInstanceReject(Runner, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.rejectprocess.error", error);
        }
    }
}
