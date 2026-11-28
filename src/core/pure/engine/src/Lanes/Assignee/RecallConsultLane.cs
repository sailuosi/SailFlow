using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>收回沟通：取消源 Task 下仍打开的参与者待办。</summary>
    public sealed class RecallConsultLane : AssigneeLaneBase
    {
        public RecallConsultLane(SailClient sail, SQLBuilder kit, FlowCommandContext context)
            : base(sail, kit, context)
        {
        }

        public override FlowOperation Operation => FlowOperation.RecallConsult;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnConsultRecalled;

        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_RECALL_CONSULT_ERROR";

        protected override bool TryLoadContext(out AssigneeLaneContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.RecallConsult_ErrorArguments;
                result.Message = "RecallConsult requires TaskId.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.RecallConsult_ErrorArguments;
                result.Message = "RecallConsult task not found.";
                return false;
            }

            var activityInstance = Sail.ManagerFactory.Activities.GetById(Kit, taskView.ActivityInstanceId);
            ctx = new AssigneeLaneContext
            {
                TaskView = taskView,
                ActivityInstance = activityInstance,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                TaskId = Context.TaskId
            };
            return true;
        }

        protected override void ExecuteCore(AssigneeLaneContext ctx, WfExecutedResult result)
        {
            var managed = Sail.ManagerFactory.Tasks.RecallConsult(Kit, ctx.TaskId, Context);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "RecallConsult succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "RecallConsultProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.RecallConsult_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.RecallConsult_RuntimeError;
            if (message.Contains("no open", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.RecallConsult_NoneOpen;
            if (message.Contains("current user's", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.RecallConsult_NotMineTask;
            return WfExceptionType.RecallConsult_RuntimeError;
        }
    }
}
