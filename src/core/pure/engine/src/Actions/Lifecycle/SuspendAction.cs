using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Event;
using sailflow.engine.Factories;
using sailflow.engine.Hooks;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Lifecycle
{
    /// <summary>挂起运行中的流程实例（由待办 TaskId 定位实例）。</summary>
    public sealed class SuspendAction : LifecycleActionBase
    {
        readonly string _taskId;

        public SuspendAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner, string taskId = null)
            : base(sail, kit, runner)
        {
            _taskId = !string.IsNullOrWhiteSpace(taskId) ? taskId : runner?.TaskId;
        }

        public override FlowOperation Operation => FlowOperation.Suspend;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessSuspended;

        protected override string ErrorLogName => "WF_PROCESS_SUSPEND_ERROR";

        protected override bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (string.IsNullOrWhiteSpace(_taskId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Suspend_ErrorArguments;
                result.Message = "Suspend requires TaskId.";
                return false;
            }

            var task = Sail.ManagerFactory.Tasks.GetTask(Kit, _taskId);
            if (task == null || string.IsNullOrWhiteSpace(task.ProcessInstanceId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Suspend_ErrorArguments;
                result.Message = "Suspend task not found.";
                return false;
            }

            var pim = Sail.ManagerFactory.ProcessInstances;
            var processInstance = pim.GetById(Kit, task.ProcessInstanceId);
            ActivityInstanceEntity activityInstance = null;
            if (!string.IsNullOrWhiteSpace(task.ActivityInstanceId))
                activityInstance = Sail.ManagerFactory.Activities.GetById(Kit, task.ActivityInstanceId);

            ctx = new LifecycleActionContext
            {
                ProcessInstanceId = task.ProcessInstanceId,
                ProcessInstance = processInstance,
                TaskId = _taskId,
                ActivityInstance = activityInstance
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result)
        {
            Sail.ManagerFactory.ProcessInstances.Suspend(Kit, ctx.ProcessInstanceId, Context);
            result.Status = WfExecutedStatus.Success;
            result.Message = "Suspend succeeded.";
            result.ProcessInstanceIdStarted = ctx.ProcessInstanceId;
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "SuspendProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Suspend_RuntimeError;
        }
    }
}
