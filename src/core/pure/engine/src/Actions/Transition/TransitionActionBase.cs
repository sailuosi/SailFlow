using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Hooks;
using sailflow.engine.Logging;
using sailflow.engine.Manager;
using sailflow.engine.Result;
using sailflow.engine.Runtime;

namespace sailflow.engine.Actions.Transition
{
    /// <summary>
    /// 流转族：固化 Gate → Runtime.Execute(Core) → 成功 Hook。
    /// Service 级 AfterSuccess 由本族拥有；路径内 TaskNotifier（Mediator/ITaskManager）不在此重复。
    /// </summary>
    public abstract class TransitionActionBase : FlowActionBase
    {
        protected TransitionActionBase(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        protected abstract EventFireTypeEnum SuccessEvent { get; }

        protected virtual string ErrorLogName => "WF_TRANSITION_ACTION_ERROR";

        /// <summary>RunAuto 等内部推进可跳过 Gate；默认 false。</summary>
        protected virtual bool SkipGate => false;

        public WfExecutedResult Execute()
        {
            var result = WfExecutedResult.Default();
            try
            {
                // Factory 内常 result = new WfExecutedResult()，必须 ref 才能带回 Exception/Message
                if (!TryLoadContext(out var ctx, ref result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Transition, GetType().Name, result);
                    return result;
                }

                return RunGateCoreHook(ctx, result);
            }
            catch (Exception e)
            {
                return Fail(result, e);
            }
        }

        /// <summary>
        /// Factory 已预载（如 Run Async）时走同一 Gate→Core→Hook，避免重复 Create。
        /// </summary>
        internal WfExecutedResult ExecuteWithRuntime(IWfRuntime runtime, WfExecutedResult factoryResult)
        {
            var result = factoryResult ?? WfExecutedResult.Default();
            try
            {
                if (result.Status == WfExecutedStatus.Exception || runtime == null)
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Transition, GetType().Name, result);
                    return result;
                }

                var ctx = ToContext(runtime);
                return RunGateCoreHook(ctx, result);
            }
            catch (Exception e)
            {
                return Fail(result, e);
            }
        }

        /// <summary>同程序集 Transition 动词可用。</summary>
        internal static TransitionActionContext ToContext(IWfRuntime runtime) =>
            new()
            {
                ProcessInstanceId = runtime.ProcessInstanceId,
                RunningActivityInstance = runtime.RunningActivityInstance,
                TaskId = runtime.TaskView?.TaskId,
                Runtime = runtime
            };

        WfExecutedResult RunGateCoreHook(TransitionActionContext ctx, WfExecutedResult result)
        {
            var action = GetType().Name;
            if (!SkipGate)
            {
                if (!ConsultPendingGuard.TryAllow(Sail, Kit, ctx.TaskId, Operation, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Transition, action, result);
                    return result;
                }

                var gateCtx = BuildGateContext(ctx.ProcessInstanceId, ctx.RunningActivityInstance, ctx.TaskId);
                EnrichGateContext(gateCtx, ctx);
                if (!TryGate(gateCtx, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Transition, action, result);
                    return result;
                }
            }

            ExecuteCore(ctx, result);
            if (result.Status != WfExecutedStatus.Success)
            {
                SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Transition, action, result);
                return result;
            }

            var hookCtx = BuildHookContext(
                ResolveHookProcessInstanceId(ctx, result),
                ctx.RunningActivityInstance,
                ctx.TaskId,
                SuccessEvent);
            OnFireFlowHook(hookCtx, SuccessEvent);
            return result;
        }

        /// <summary>Start 等可补 ActivityId/Code；默认无操作。</summary>
        protected virtual void EnrichGateContext(FlowHookContext gateCtx, TransitionActionContext ctx)
        {
        }

        /// <summary>Start 成功后 PI 在 <see cref="WfExecutedResult.ProcessInstanceIdStarted"/>。</summary>
        protected virtual string ResolveHookProcessInstanceId(TransitionActionContext ctx, WfExecutedResult result)
            => ctx.ProcessInstanceId;

        WfExecutedResult Fail(WfExecutedResult result, Exception e)
        {
            result.Status = WfExecutedStatus.Failed;
            result.Message = FormatFailedMessage(e);
            ApplyFailedExceptionType(result, e);
            SailFailLog.Exception(Sail, SailLogCategories.Transition, GetType().Name, e, result.Message);
            ILogManager.RecordLog(Sail, ErrorLogName, LogEventType.Error, LogPriority.High, Context, e);
            return result;
        }

        protected abstract bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result);

        protected abstract void ExecuteCore(TransitionActionContext ctx, WfExecutedResult result);

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
