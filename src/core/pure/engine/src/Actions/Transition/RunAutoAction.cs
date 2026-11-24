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
    /// <summary>
    /// 自动推进：CreateRuntimeInstanceRunAuto →（无 Gate）→ Execute → OnProcessContinued。
    /// 保留旧语义：不走 Gate；Operation 仍为 <see cref="FlowOperation.Run"/>。
    /// </summary>
    public sealed class RunAutoAction : TransitionActionBase
    {
        public RunAutoAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Run;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessContinued;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_RUN_ERROR;
        protected override bool SkipGate => true;

        protected override bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceRunAuto(Context, Kit, ref result);
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
