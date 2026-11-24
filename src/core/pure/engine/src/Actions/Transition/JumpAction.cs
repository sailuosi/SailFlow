using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.events;
using sailflow.engine.Factories;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.lanes
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

        protected override bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            Context.NextActivityPerformers = TransitionNextPerformers.Fill(Sail, Context, _jumpOption);
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceJump(Context, ref result);
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
