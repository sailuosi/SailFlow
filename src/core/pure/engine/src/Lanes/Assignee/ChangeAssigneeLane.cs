using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>
    /// 改派：用 <c>NextActivityPerformers</c> 作为新办理人全集，差量关旧/建新；不推进下一节点。
    /// 定位活动靠 <c>TaskId</c>。
    /// </summary>
    public sealed class ChangeAssigneeLane : AssigneeLaneBase
    {
        PerformerList _targets;

        public ChangeAssigneeLane(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.ChangeAssignee;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnAssigneeChanged;

        /// <summary><see cref="Manager.ITaskManager.ChangeAssignees"/> 内已发 Cancelled/Created。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_CHANGE_ASSIGNEE_ERROR";

        protected override bool TryLoadContext(out AssigneeLaneContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.ChangeAssignee_ErrorArguments;
                result.Message = "ChangeAssignee requires TaskId.";
                return false;
            }

            _targets = TransferLane.FlattenTransferTargets(Context);
            if (_targets.Count == 0)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.ChangeAssignee_NoneTargets;
                result.Message = "ChangeAssignee requires NextActivityPerformers with at least one user.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.ChangeAssignee_ErrorArguments;
                result.Message = "ChangeAssignee task not found.";
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
            var managed = Sail.ManagerFactory.Tasks.ChangeAssignees(Kit, ctx.TaskId, Context, _targets);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "ChangeAssignee succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "ChangeAssigneeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.ChangeAssignee_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.ChangeAssignee_RuntimeError;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.ChangeAssignee_NotInRunning;
            if (message.Contains("target", StringComparison.OrdinalIgnoreCase)
                || message.Contains("performer", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.ChangeAssignee_NoneTargets;
            return WfExceptionType.ChangeAssignee_RuntimeError;
        }
    }
}
