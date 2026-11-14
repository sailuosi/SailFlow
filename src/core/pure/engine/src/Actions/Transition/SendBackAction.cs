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
    /// <summary>退回：CreateRuntimeInstanceSendBack → Execute；族群固化 Gate/成功 Hook。</summary>
    public sealed class SendBackAction : TransitionActionBase
    {
        public SendBackAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.SendBack;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessSendBacked;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_SENDBACK_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            var runtime = SailFactoryAccess.Runtime(Sail).CreateRuntimeInstanceSendBack(Runner, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.sendbackprocess.error", error);
        }
    }
}
