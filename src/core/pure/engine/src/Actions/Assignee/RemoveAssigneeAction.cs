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
    /// 减签：从当前多人节点移除指定未办办理人，不推进下一节点。
    /// 目标人取自 <c>NextActivityPerformers</c>（任意键下的 Performer）；定位活动靠 <c>TaskId</c>。
    /// </summary>
    public sealed class RemoveAssigneeAction : AssigneeActionBase
    {
        PerformerList _targets;

        public RemoveAssigneeAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.RemoveAssignee;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnAssigneeRemoved;

        /// <summary><see cref="Manager.ITaskManager.RemoveAssignees"/> 内已 FireTaskLifecycle(Cancelled)。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_REMOVE_ASSIGNEE_ERROR";

        protected override bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.RemoveAssignee_ErrorArguments;
                result.Message = "RemoveAssignee requires TaskId.";
                return false;
            }

            _targets = TransferAction.FlattenTransferTargets(Context);
            if (_targets.Count == 0)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.RemoveAssignee_NoneTargets;
                result.Message = "RemoveAssignee requires NextActivityPerformers with at least one user.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.RemoveAssignee_ErrorArguments;
                result.Message = "RemoveAssignee task not found.";
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
            var managed = Sail.ManagerFactory.Tasks.RemoveAssignees(Kit, ctx.TaskId, Context, _targets);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "RemoveAssignee succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "RemoveAssigneeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.RemoveAssignee_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.RemoveAssignee_RuntimeError;
            if (message.Contains("last open", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.RemoveAssignee_LastAssignee;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.RemoveAssignee_NotInRunning;
            if (message.Contains("target", StringComparison.OrdinalIgnoreCase)
                || message.Contains("performer", StringComparison.OrdinalIgnoreCase)
                || message.Contains("matched", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.RemoveAssignee_NoneTargets;
            return WfExceptionType.RemoveAssignee_RuntimeError;
        }
    }
}
