using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.engine.Logging;
using sailflow.data;
using sailflow.engine.Result;

namespace sailflow.lanes
{
    /// <summary>
    /// 待办人/办理人变化族：固化 Gate → Core → 成功 Hook（→ 可选 TaskLifecycle）时序。
    /// 动词子类只实现 <see cref="ExecuteCore"/> 与身份属性。
    /// </summary>
    public abstract class AssigneeActionBase : FlowActionBase
    {
        protected AssigneeActionBase(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        /// <summary>本动词成功后的流程级 Hook 事件。</summary>
        protected abstract EventFireTypeEnum SuccessEvent { get; }

        /// <summary>
        /// 族群时序第 5 步：是否在 Core 成功后再 FireTaskLifecycle。
        /// 默认 false——现行 Manager（如 Transfer）常已在写库路径内通知；新动词若 Manager 不通知可 Override 为 true。
        /// </summary>
        protected virtual bool FireTaskLifecycleAfterCore => false;

        /// <summary>日志用错误事件名（可选）。</summary>
        protected virtual string ErrorLogName => "WF_ASSIGNEE_ACTION_ERROR";

        public WfExecutedResult Execute()
        {
            var result = WfExecutedResult.Default();
            var action = GetType().Name;
            try
            {
                if (!TryLoadContext(out var ctx, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.AssigneeAction, action, result);
                    return result;
                }

                if (!ConsultPendingGuard.TryAllow(Sail, Kit, ctx.TaskId, Operation, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.AssigneeAction, action, result);
                    return result;
                }

                var gateCtx = BuildGateContext(ctx.ProcessInstanceId, ctx.ActivityInstance, ctx.TaskId);
                if (!TryGate(gateCtx, result))
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.AssigneeAction, action, result);
                    return result;
                }

                ExecuteCore(ctx, result);
                if (result.Status != WfExecutedStatus.Success)
                {
                    SailFailLog.ResultNotSuccess(Sail, SailLogCategories.AssigneeAction, action, result);
                    return result;
                }

                var hookCtx = BuildHookContext(
                    ctx.ProcessInstanceId,
                    ctx.ActivityInstance,
                    ctx.TaskId,
                    SuccessEvent);
                OnFireFlowHook(hookCtx, SuccessEvent);

                if (FireTaskLifecycleAfterCore)
                    OnFireTaskLifecycleAfterCore(ctx);
            }
            catch (Exception e)
            {
                result.Status = WfExecutedStatus.Failed;
                result.Message = FormatFailedMessage(e);
                ApplyFailedExceptionType(result, e);
                SailFailLog.Exception(Sail, SailLogCategories.AssigneeAction, action, e, result.Message);
                ILogManager.RecordLog(Sail, ErrorLogName, LogEventType.Error, LogPriority.High, Context, e);
            }

            return result;
        }

        /// <summary>装载 Task+AI；失败时写好 <paramref name="result"/> 并返回 false。</summary>
        protected abstract bool TryLoadContext(out AssigneeActionContext ctx, WfExecutedResult result);

        /// <summary>仅写业务：改待办/办理人。成功时将 <paramref name="result"/>.Status 设为 Success。</summary>
        protected abstract void ExecuteCore(AssigneeActionContext ctx, WfExecutedResult result);

        /// <summary>族内第 5 步默认空实现；需要时 Override。</summary>
        protected virtual void OnFireTaskLifecycleAfterCore(AssigneeActionContext ctx)
        {
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
