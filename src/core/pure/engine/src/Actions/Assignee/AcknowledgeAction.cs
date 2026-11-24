using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>阅办：参与者办结自己的沟通待办。</summary>
    public sealed class AcknowledgeAction : AssigneeLaneBase
    {
        public AcknowledgeAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Acknowledge;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnTaskAcknowledged;

        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_ACKNOWLEDGE_ERROR";

        protected override bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Acknowledge_ErrorArguments;
                result.Message = "Acknowledge requires TaskId.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Acknowledge_ErrorArguments;
                result.Message = "Acknowledge task not found.";
                return false;
            }

            if (!AssigneeRoleHelper.IsParticipant(taskView.AssigneeRole))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Acknowledge_NotParticipant;
                result.Message = "Acknowledge requires a participant task.";
                return false;
            }

            var activityInstance = Sail.ManagerFactory.Activities.GetById(Kit, taskView.ActivityInstanceId);
            ctx = new AssigneeActionContext
            {
                TaskView = taskView,
                ActivityInstance = activityInstance,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                TaskId = Context.TaskId
            };
            return true;
        }

        protected override void ExecuteCore(AssigneeActionContext ctx, WfExecutedResult result)
        {
            var managed = Sail.ManagerFactory.Tasks.Acknowledge(Kit, ctx.TaskId, Context);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "Acknowledge succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "AcknowledgeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Acknowledge_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.Acknowledge_RuntimeError;
            if (message.Contains("participant", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Acknowledge_NotParticipant;
            if (message.Contains("current user's", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Acknowledge_NotMineTask;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Acknowledge_NotInRunning;
            return WfExceptionType.Acknowledge_RuntimeError;
        }
    }
}
