using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.engine.Factories;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>关闭：填 End 办理人 → CreateRuntimeInstanceClose → Execute。</summary>
    public sealed class CloseAction : TransitionActionBase
    {
        public CloseAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Close;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessClosed;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_CLOSE_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            Context.NextActivityPerformers = TransitionNextPerformers.Fill(Sail, Context, JumpOptionEnum.End);
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceClose(Context, ref result);
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
            return Sail.Messages.GetEngineMessage("workflowservice.closeprocess.error", error);
        }
    }
}
