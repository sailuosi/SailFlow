using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.events;
using sailflow.factory;
using sailflow.hooks;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>恢复已挂起的流程实例（由 ProcessInstanceId 定位）。</summary>
    public sealed class ResumeAction : LifecycleActionBase
    {
        readonly string _processInstanceId;

        public ResumeAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner, string processInstanceId = null)
            : base(sail, kit, runner)
        {
            _processInstanceId = !string.IsNullOrWhiteSpace(processInstanceId)
                ? processInstanceId
                : runner?.ProcessInstanceId;
        }

        public override FlowOperation Operation => FlowOperation.Resume;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessResumed;

        protected override string ErrorLogName => "WF_PROCESS_RESUME_ERROR";

        protected override bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (string.IsNullOrWhiteSpace(_processInstanceId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Resume_ErrorArguments;
                result.Message = "Resume requires ProcessInstanceId.";
                return false;
            }

            var processInstance = Sail.ManagerFactory.ProcessInstances.GetById(Kit, _processInstanceId);
            if (processInstance == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Resume_ErrorArguments;
                result.Message = "Resume process instance not found.";
                return false;
            }

            ctx = new LifecycleActionContext
            {
                ProcessInstanceId = _processInstanceId,
                ProcessInstance = processInstance,
                TaskId = Context?.TaskId,
                ActivityInstance = null
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result)
        {
            Sail.ManagerFactory.ProcessInstances.Resume(Kit, ctx.ProcessInstanceId, Context);
            result.Status = WfExecutedStatus.Success;
            result.Message = "Resume succeeded.";
            result.ProcessInstanceIdStarted = ctx.ProcessInstanceId;
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "ResumeProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Resume_RuntimeError;
        }
    }
}
