using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Factories;
using sailflow.engine.Hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Lifecycle
{
    /// <summary>取消运行中的流程实例（按 BizPK+ProcessId+Version 定位，同旧 PIM.Cancel）。</summary>
    public sealed class CancelAction : LifecycleActionBase
    {
        public CancelAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Cancel;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessCanceled;

        protected override string ErrorLogName => "WF_PROCESS_CANCEL_ERROR";

        protected override bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (string.IsNullOrWhiteSpace(Runner?.BizPK)
                || string.IsNullOrWhiteSpace(Runner?.ProcessId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Cancel_ErrorArguments;
                result.Message = "Cancel requires BizPK and ProcessId.";
                return false;
            }

            var pim = SailFactoryAccess.Managers(Sail).ProcessInstances;
            var processInstance = pim.GetProcessInstanceLatest(Kit, Runner.BizPK, Runner.ProcessId, Runner.Version);
            if (processInstance == null || string.IsNullOrWhiteSpace(processInstance.ProcessInstanceId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Cancel_ErrorArguments;
                result.Message = "Cancel process instance not found.";
                return false;
            }

            ctx = new LifecycleActionContext
            {
                ProcessInstanceId = processInstance.ProcessInstanceId,
                ProcessInstance = processInstance,
                TaskId = Runner?.TaskId,
                ActivityInstance = null
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result)
        {
            SailFactoryAccess.Managers(Sail).ProcessInstances.Cancel(Kit, Runner);
            result.Status = WfExecutedStatus.Success;
            result.Message = "Cancel succeeded.";
            result.ProcessInstanceIdStarted = ctx.ProcessInstanceId;
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "CancelProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Cancel_RuntimeError;
        }
    }
}
