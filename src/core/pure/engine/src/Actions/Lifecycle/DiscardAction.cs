using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.engine.Factories;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Lifecycle
{
    /// <summary>作废流程实例（按 BizPK+ProcessId+Version 批量更新，同旧 PIM.Discard）。</summary>
    public sealed class DiscardAction : LifecycleActionBase
    {
        public DiscardAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Discard;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessDiscarded;

        protected override string ErrorLogName => "WF_PROCESS_DISCARD_ERROR";

        protected override bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (string.IsNullOrWhiteSpace(Context?.BizPK)
                || string.IsNullOrWhiteSpace(Context?.ProcessId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Discard_ErrorArguments;
                result.Message = "Discard requires BizPK and ProcessId.";
                return false;
            }

            var pim = Sail.ManagerFactory.ProcessInstances;
            var processInstance = pim.GetProcessInstanceLatest(Kit, Context.BizPK, Context.ProcessId, Context.Version);
            // Discard 旧语义：无行也可 doUpdate；Gate/Hook 尽量带真实 PI，否则用 Runner 占位
            ctx = new LifecycleActionContext
            {
                ProcessInstanceId = processInstance?.ProcessInstanceId
                    ?? Context.ProcessInstanceId
                    ?? string.Empty,
                ProcessInstance = processInstance,
                TaskId = Context?.TaskId,
                ActivityInstance = null
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result)
        {
            Sail.ManagerFactory.ProcessInstances.Discard(Kit, Context);
            result.Status = WfExecutedStatus.Success;
            result.Message = "Discard succeeded.";
            result.ProcessInstanceIdStarted = ctx.ProcessInstance?.ProcessInstanceId;
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "DiscardProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Discard_RuntimeError;
        }
    }
}
