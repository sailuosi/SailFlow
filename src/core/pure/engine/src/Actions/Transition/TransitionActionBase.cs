using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Manager;
using sailflow.engine.Result;

namespace sailflow.engine.Actions.Transition
{
    /// <summary>
    /// 流转族：固化 Gate → Runtime.Execute(Core) → 成功 Hook。
    /// Service 级 AfterSuccess 由本族拥有；路径内 TaskNotifier（Mediator/TaskManager）不在此重复。
    /// </summary>
    public abstract class TransitionActionBase : FlowActionBase
    {
        protected TransitionActionBase(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        protected abstract EventFireTypeEnum SuccessEvent { get; }

        protected virtual string ErrorLogName => "WF_TRANSITION_ACTION_ERROR";

        public WfExecutedResult Execute()
        {
            var result = WfExecutedResult.Default();
            try
            {
                if (!TryLoadContext(out var ctx, result))
                    return result;

                var gateCtx = BuildGateContext(ctx.ProcessInstanceId, ctx.RunningActivityInstance, ctx.TaskId);
                if (!TryGate(gateCtx, result))
                    return result;

                ExecuteCore(ctx, result);
                if (result.Status != WfExecutedStatus.Success)
                    return result;

                var hookCtx = BuildHookContext(
                    ctx.ProcessInstanceId,
                    ctx.RunningActivityInstance,
                    ctx.TaskId,
                    SuccessEvent);
                OnFireFlowHook(hookCtx, SuccessEvent);
            }
            catch (Exception e)
            {
                result.Status = WfExecutedStatus.Failed;
                result.Message = FormatFailedMessage(e);
                ApplyFailedExceptionType(result, e);
                LogManager.RecordLog(Sail, ErrorLogName, LogEventType.Error, LogPriority.High, Runner, e);
            }

            return result;
        }

        /// <summary>创建/装载 Runtime；失败时 result 已写好（常为 Factory 预检 Exception）。</summary>
        protected abstract bool TryLoadContext(out TransitionActionContext ctx, WfExecutedResult result);

        /// <summary>执行 Runtime（或专用 Maker）；成功时同步 result 自 Runtime.WfExecutedResult。</summary>
        protected abstract void ExecuteCore(TransitionActionContext ctx, WfExecutedResult result);

        /// <summary>执行 Runtime 并将结果拷入命令结果（含 Assignee ReasonCode / 退回接收人等）。</summary>
        protected void ExecuteRuntimeAndCopyResult(TransitionActionContext ctx, WfExecutedResult result)
        {
            ctx.Runtime.Execute(Kit);
            var executed = ctx.Runtime.WfExecutedResult;
            if (executed == null)
                return;

            result.Status = executed.Status;
            result.Message = executed.Message;
            result.ExceptionType = executed.ExceptionType;
            result.ReasonCode = executed.ReasonCode;
            result.ValidationCode = executed.ValidationCode;
            result.ValidationDetails = executed.ValidationDetails;
            result.BackwardTaskReceiver = executed.BackwardTaskReceiver;
            result.ReturnDataContext = executed.ReturnDataContext;
            result.ProcessInstanceIdStarted = executed.ProcessInstanceIdStarted;
            result.AiResponse = executed.AiResponse;
        }

        protected virtual string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return GetType().Name + " error: " + error;
        }

        protected virtual void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
        }
    }
}
