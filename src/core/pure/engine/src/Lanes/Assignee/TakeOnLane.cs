using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.result;
using sailflow.data;

namespace sailflow.lanes
{
    /// <summary>
    /// 认领/承办：或签（Race/GroupRace）下独占待办，旁关他人（或同组），不推进下一节点。
    /// </summary>
    public sealed class TakeOnLane : AssigneeLaneBase
    {
        public TakeOnLane(SailClient sail, SQLBuilder kit, FlowCommandContext context)
            : base(sail, kit, context)
        {
        }

        public override FlowOperation Operation => FlowOperation.TakeOn;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnAssigneeTakeOn;

        /// <summary><see cref="Manager.ITaskManager.TakeOnExclusive"/> 内已 FireTaskLifecycle(Cancelled)。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_TAKEON_ERROR";

        protected override bool TryLoadContext(out AssigneeLaneContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_ErrorArguments;
                result.Message = "TakeOn requires TaskId.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_ErrorArguments;
                result.Message = "TakeOn task not found.";
                return false;
            }

            if (AssigneeRoleHelper.IsParticipant(taskView.AssigneeRole))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_ErrorArguments;
                result.Message = "TakeOn cannot start from a participant task.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Context.UserId)
                && !string.Equals(taskView.SignId, Context.UserId, StringComparison.OrdinalIgnoreCase))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_NotMineTask;
                result.Message = "TakeOn requires the current user's to-do.";
                return false;
            }

            var activityInstance = Sail.ManagerFactory.Activities.GetById(Kit, taskView.ActivityInstanceId);
            if (activityInstance == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_ErrorArguments;
                result.Message = "TakeOn activity not found.";
                return false;
            }

            if (activityInstance.ActivityState != (short)ActivityStateEnum.Ready
                && activityInstance.ActivityState != (short)ActivityStateEnum.Running)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_NotInRunning;
                result.Message = "TakeOn activity is not ready/running.";
                return false;
            }

            var leaveType = ResolveLeaveType(activityInstance);
            if (!TakeOnPeerHelper.IsTakeOnLeaveAllowed(leaveType))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.TakeOn_LeaveNotAllowed;
                result.ReasonCode = WfExceptionType.TakeOnLeaveNotAllowed;
                result.Message = "TakeOn requires Leave=Race or GroupRace.";
                return false;
            }

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
            var managed = Sail.ManagerFactory.Tasks.TakeOnExclusive(Kit, ctx.TaskId, Context);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                if (string.Equals(result.ExceptionType, WfExceptionType.TakeOn_LeaveNotAllowed, StringComparison.Ordinal))
                    result.ReasonCode = WfExceptionType.TakeOnLeaveNotAllowed;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "TakeOn succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "TakeOnProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.TakeOn_RuntimeError;
        }

        string ResolveLeaveType(ActivityInstanceEntity activityInstance)
        {
            if (!string.IsNullOrWhiteSpace(activityInstance.LeaveJudgeType))
                return activityInstance.LeaveJudgeType;

            if (!string.IsNullOrWhiteSpace(activityInstance.MainActInstanceId)
                && !string.Equals(
                    activityInstance.MainActInstanceId,
                    activityInstance.ActivityInstanceId,
                    StringComparison.OrdinalIgnoreCase))
            {
                var main = Sail.ManagerFactory.Activities.GetById(Kit, activityInstance.MainActInstanceId);
                if (!string.IsNullOrWhiteSpace(main?.LeaveJudgeType))
                    return main.LeaveJudgeType;
            }

            return activityInstance.LeaveJudgeType;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.TakeOn_RuntimeError;
            if (message.Contains("Leave", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Race", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.TakeOn_LeaveNotAllowed;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.TakeOn_NotInRunning;
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("TaskId", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.TakeOn_ErrorArguments;
            if (message.Contains("current user", StringComparison.OrdinalIgnoreCase)
                || message.Contains("mine", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.TakeOn_NotMineTask;
            return WfExceptionType.TakeOn_RuntimeError;
        }
    }
}
