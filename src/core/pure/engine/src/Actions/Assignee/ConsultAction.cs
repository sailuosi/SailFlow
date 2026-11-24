using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>
    /// 沟通：为选中人建参与者待办（role=1），锁定发起人审批直至全员阅办或收回。
    /// </summary>
    public sealed class ConsultAction : AssigneeActionBase
    {
        PerformerList _targets;

        public ConsultAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Consult;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessConsulted;

        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_CONSULT_ERROR";

        protected override bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || string.IsNullOrWhiteSpace(Context.TaskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Consult_ErrorArguments;
                result.Message = "Consult requires TaskId.";
                return false;
            }

            _targets = TransferAction.FlattenTransferTargets(Context);
            if (_targets.Count == 0)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Consult_NoneTargets;
                result.Message = "Consult requires NextActivityPerformers with at least one user.";
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Consult_ErrorArguments;
                result.Message = "Consult task not found.";
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
            var managed = Sail.ManagerFactory.Tasks.Consult(Kit, ctx.TaskId, Context, _targets);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = "Consult succeeded.";
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "ConsultProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Consult_RuntimeError;
        }

        static string MapException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.Consult_RuntimeError;
            if (message.Contains("current user's", StringComparison.OrdinalIgnoreCase)
                || message.Contains("self", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Consult_NotMineTask;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase)
                || message.Contains("to-do", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Consult_NotInRunning;
            if (message.Contains("target", StringComparison.OrdinalIgnoreCase)
                || message.Contains("participant", StringComparison.OrdinalIgnoreCase)
                || message.Contains("performer", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Consult_NoneTargets;
            return WfExceptionType.Consult_RuntimeError;
        }
    }
}
