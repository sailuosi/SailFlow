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
    /// <summary>终止流程实例（ProcessInstanceId 或 BizPK+ProcessId+Version；级联收口待办）。</summary>
    public sealed class TerminateAction : LifecycleActionBase
    {
        public TerminateAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Terminate;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessTerminated;

        protected override string ErrorLogName => "WF_PROCESS_TERMINATE_ERROR";

        protected override bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            var pim = Sail.ManagerFactory.ProcessInstances;
            ProcessInstanceEntity processInstance;
            if (!string.IsNullOrWhiteSpace(Context?.ProcessInstanceId))
                processInstance = pim.GetById(Kit, Context.ProcessInstanceId);
            else if (!string.IsNullOrWhiteSpace(Context?.BizPK) && !string.IsNullOrWhiteSpace(Context?.ProcessId))
                processInstance = pim.GetProcessInstanceLatest(Kit, Context.BizPK, Context.ProcessId, Context.Version);
            else
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Terminate_ErrorArguments;
                result.Message = "Terminate requires ProcessInstanceId or BizPK+ProcessId.";
                return false;
            }

            if (processInstance == null || string.IsNullOrWhiteSpace(processInstance.ProcessInstanceId))
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Terminate_ErrorArguments;
                result.Message = "Terminate process instance not found.";
                return false;
            }

            ctx = new LifecycleActionContext
            {
                ProcessInstanceId = processInstance.ProcessInstanceId,
                ProcessInstance = processInstance,
                TaskId = Context?.TaskId,
                ActivityInstance = null
            };
            return true;
        }

        protected override void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result)
        {
            // PIM 内级联 Cancelled Notifier；命令级 OnProcessTerminated 仍由族基类 Hook
            Sail.ManagerFactory.ProcessInstances.Terminate(Kit, ctx.ProcessInstance, Context);
            result.Status = WfExecutedStatus.Success;
            result.Message = "Terminate succeeded.";
            result.ProcessInstanceIdStarted = ctx.ProcessInstanceId;
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return "TerminateProcess error: " + error;
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Terminate_RuntimeError;
        }
    }
}
