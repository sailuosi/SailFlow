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
    /// <summary>同意/推进：CreateRuntimeInstanceAppRunning(kit) → Gate → Execute → OnProcessContinued。</summary>
    public sealed class RunAction : TransitionActionBase
    {
        public RunAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Run;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessContinued;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_RUN_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            var runtime = SailFactoryAccess.Runtime(Sail).CreateRuntimeInstanceAppRunning(Runner, Kit, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
        }
    }
}
