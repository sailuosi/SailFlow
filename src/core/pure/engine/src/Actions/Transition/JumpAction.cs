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
    /// <summary>跳转：按 JumpOption 填办理人 → CreateRuntimeInstanceJump → Execute。</summary>
    public sealed class JumpAction : TransitionActionBase
    {
        readonly JumpOptionEnum _jumpOption;

        public JumpAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default)
            : base(sail, kit, runner)
        {
            _jumpOption = jumpOption;
        }

        public override FlowOperation Operation => FlowOperation.Jump;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessJumped;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_JUMP_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            Runner.NextActivityPerformers = TransitionNextPerformers.Fill(Sail, Runner, _jumpOption);
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceJump(Runner, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.jumpprocess.error", error);
        }
    }
}
