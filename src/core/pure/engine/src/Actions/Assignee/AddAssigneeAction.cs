using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>
    /// 加人：向当前扁节点追加普通办理人待办，不推进下一节点、不压加签栈。
    /// 目标人取自 <c>NextActivityPerformers</c>（增量）；定位活动靠 <c>TaskId</c>。
    /// </summary>
    public sealed class AddAssigneeAction : AssigneeActionBase
    {
        PerformerList _targets;

        public AddAssigneeAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.AddAssignee;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnAssigneeAdded;

        /// <summary><see cref="Manager.ITaskManager.AddAssignees"/> 内已 FireTaskLifecycle(Created)。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_ADD_ASSIGNEE_ERROR";

        protected override bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.AddAssignee_ErrorArguments;
                result.Message = "AddAssignee requires TaskId.";
                return false;
            }

            _targets = TransferAction.FlattenTransferTargets(Context);
            if (_targets.Count == 0)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.AddAssignee_NoneTargets;
                result.Message = "AddAssignee requires NextActivityPerformers with at least one user.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.AddAssignee_ErrorArguments;
                result.Message = "AddAssignee task not found.";
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
            var managed = Sail.ManagerFactory.Tasks.AddAssignees(Kit, ctx.TaskId, Context, _targets);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "AddAssignee succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "AddAssigneeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.AddAssignee_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.AddAssignee_RuntimeError;
            if (message.Contains("flat", StringComparison.OrdinalIgnoreCase)
                || message.Contains("SignTogether", StringComparison.OrdinalIgnoreCase)
                || message.Contains("SignForward", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.AddAssignee_NotFlatNode;
            if (message.Contains("already open", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.AddAssignee_AlreadyAssignee;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.AddAssignee_NotInRunning;
            if (message.Contains("target", StringComparison.OrdinalIgnoreCase)
                || message.Contains("performer", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.AddAssignee_NoneTargets;
            return WfExceptionType.AddAssignee_RuntimeError;
        }
    }
}
