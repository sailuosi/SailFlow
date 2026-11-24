using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Assignee
{
    /// <summary>
    /// 催办：对当前节点未办待办发 <see cref="TaskNotifyKind.Urged"/>，不改状态、不推进。
    /// <c>TaskId</c> 定位活动；可选 <c>NextActivityPerformers</c> 限定催办对象（空=全部打开待办）。
    /// </summary>
    public sealed class UrgeAction : AssigneeActionBase
    {
        HashSet<string> _filterUserIds;

        public UrgeAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Urge;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessUrged;

        /// <summary>Manager 内已逐条 Urged Notifier。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_URGE_ERROR";

        protected override bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Urge_ErrorArguments;
                result.Message = "Urge requires TaskId.";
                return false;
            }

            var targets = TransferAction.FlattenTransferTargets(Context);
            _filterUserIds = null;
            if (targets.Count > 0)
            {
                _filterUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in targets)
                {
                    if (!string.IsNullOrWhiteSpace(p?.UserId))
                        _filterUserIds.Add(p.UserId);
                }
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Urge_ErrorArguments;
                result.Message = "Urge task not found.";
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
            var managed = Sail.ManagerFactory.Tasks.UrgeOpenTasks(Kit, ctx.TaskId, Context, _filterUserIds);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "Urge succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "UrgeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Urge_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.Urge_RuntimeError;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Urge_NotInRunning;
            if (message.Contains("no open", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Urge_NoOpenTasks;
            return WfExceptionType.Urge_RuntimeError;
        }
    }
}
