using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Pattern;
using saiflow.engine.Result;
using saiflow.engine.Event;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Core;
using saiflow.engine.Hooks;

using saiflow.engine.Factories;
namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Runtimer Manager Run
    /// 应用执行运行时
    /// </summary>
    internal class WfRuntimeManagerRun : WfRuntimeManager
    {
        /// <summary>
        /// Run execute method
        /// 运行执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            var result = base.WfExecutedResult;
            result.ProcessInstanceIdStarted = RunningActivityInstance.ProcessInstanceId;

            try
            {
                ActivityForwardContext runningExecutionContext = null;
                if (base.TaskView != null)
                {
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit, Sail);
                }
                else
                {
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByActivity(base.RunningActivityInstance,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit, Sail);
                }

                // 推进前校验已由 WorkflowService.TryGate 完成；不再触发 EventSubscription OnProcessRunning

                NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(runningExecutionContext, kit);
                mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
                mediator.ExecuteWorkItem(null);

                FireProcessCompletedHooks(kit);

                result.Status = WfExecutedStatus.Success;
                result.Message = mediator.GetNodeMediatedMessage();
            }
            catch (WfRuntimeException rx)
            {
                if (rx is WfAssigneeException ax)
                    result.ApplyAssigneeException(ax);
                else
                {
                    result.Status = WfExecutedStatus.Failed;
                    result.ExceptionType = WfExceptionType.RunApp_RuntimeError;
                    result.Message = rx.Message;
                }
                throw;
            }
            catch (System.Exception ex)
            {
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.RunApp_RuntimeError;
                result.Message = ex.Message;
                throw;
            }
        }

        /// <summary>实例已 Completed 时走 Gate/Hook 三轨的 OnProcessCompleted（不再走 EventSubscriptionList）。</summary>
        private void FireProcessCompletedHooks(SQLBuilder kit)
        {
            var pim = Sail.ProcessInstances;
            var entity = pim.GetById(kit, RunningActivityInstance.ProcessInstanceId);
            if (entity == null || entity.ProcessState != (short)ProcessStateEnum.Completed)
                return;

            var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                Sail,
                kit,
                AppRunner,
                RunningActivityInstance.ProcessInstanceId,
                RunningActivityInstance,
                TaskView?.Id,
                EventFireTypeEnum.OnProcessCompleted,
                FlowOperation.Run);
            FlowCapabilityPipeline.AfterSuccess(
                Sail, kit, hookCtx, EventFireTypeEnum.OnProcessCompleted);
        }
    }
}
