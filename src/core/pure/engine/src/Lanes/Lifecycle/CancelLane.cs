using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.factory;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>取消运行中的流程实例（按 BizPK+ProcessId+Version 定位，同旧 PIM.Cancel）。</summary>
    public sealed class CancelLane : LifecycleLaneBase
    {
        public CancelLane(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Cancel;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessCanceled;

        protected override string ErrorLogName => "WF_PROCESS_CANCEL_ERROR";

        protected override bool TryLoadContext(out LifecycleLaneContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (string.IsNullOrWhiteSpace(Context?.BizPK)
                || string.IsNullOrWhiteSpace(Context?.ProcessId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Cancel_ErrorArguments;
                result.Message = "Cancel requires BizPK and ProcessId.";
                return false;
            }

            var pim = Sail.ManagerFactory.ProcessInstances;
            var processInstance = pim.GetProcessInstanceLatest(Kit, Context.BizPK, Context.ProcessId, Context.Version);
            if (processInstance == null || string.IsNullOrWhiteSpace(processInstance.ProcessInstanceId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Cancel_ErrorArguments;
                result.Message = "Cancel process instance not found.";
                return false;
            }

            ctx = new LifecycleLaneContext
            {
                ProcessInstanceId = processInstance.ProcessInstanceId,
                ProcessInstance = processInstance,
                TaskId = Context?.TaskId,
                ActivityInstance = null
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleLaneContext ctx, WfExecutedResult result)
        {
            Sail.ManagerFactory.ProcessInstances.Cancel(Kit, Context);
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
