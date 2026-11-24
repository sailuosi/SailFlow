using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.log;
using sailflow.data;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>
    /// 流程实例状态变化族：固化 Gate → Core → 成功 Hook；默认不 FireTaskLifecycle。
    /// </summary>
    public abstract class LifecycleActionBase : FlowActionBase
    {
        protected LifecycleActionBase(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        protected abstract EventFireTypeEnum SuccessEvent { get; }

        protected virtual string ErrorLogName => "WF_LIFECYCLE_ACTION_ERROR";

        public WfExecutedResult Execute()
        {
            var result = WfExecutedResult.Default();
            var action = GetType().Name;
            try
            {
                if (!TryLoadContext(out var ctx, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Lifecycle, action, result);
                    return result;
                }

                var gateCtx = BuildGateContext(ctx.ProcessInstanceId, ctx.ActivityInstance, ctx.TaskId);
                if (!TryGate(gateCtx, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Lifecycle, action, result);
                    return result;
                }

                ExecuteCore(ctx, result);
                if (result.Status != WfExecutedStatus.Success)
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.Lifecycle, action, result);
                    return result;
                }

                var hookCtx = BuildHookContext(
                    ctx.ProcessInstanceId,
                    ctx.ActivityInstance,
                    ctx.TaskId,
                    SuccessEvent);
                OnFireFlowHook(hookCtx, SuccessEvent);
            }
            catch (Exception e)
            {
                result.Status = WfExecutedStatus.Failed;
                result.Message = FormatFailedMessage(e);
                ApplyFailedExceptionType(result, e);
                SailFailLog.Exception(Sail, SailLogCategories.Lifecycle, action, e, result.Message);
                ILogManager.RecordLog(Sail, ErrorLogName, LogEventType.Error, LogPriority.High, Context, e);
            }

            return result;
        }

        protected abstract bool TryLoadContext(out LifecycleActionContext ctx, WfExecutedResult result);

        protected abstract void ExecuteCore(LifecycleActionContext ctx, WfExecutedResult result);

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
