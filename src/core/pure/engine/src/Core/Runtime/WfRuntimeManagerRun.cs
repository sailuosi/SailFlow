using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Pattern;
using saiflow.engine.Result;
using saiflow.engine.Event;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Core;

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
            result.ProcessInstanceIdStarted = RunningActivityInstance.ProcessInstanceId.Value;

            try
            {
                ActivityForwardContext runningExecutionContext = null;
                if (base.TaskView != null)
                {
                    //有TaskID的任务类型执行上下文
                    //Task type execution context with TaskId
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit, Sail);
                }
                else
                {
                    //Interrupt事件类型的活动执行上下文
                    //Interrupt event type activity execution context
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByActivity(base.RunningActivityInstance,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit, Sail);
                }

                OnProcessRunning(kit);

                NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(runningExecutionContext, kit, Sail);
                mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
                mediator.ExecuteWorkItem(null);

                OnProcessCompleted(kit);
               
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

        /// <summary>
        /// Process Running event
        /// 执行流程时的绑定事件
        /// </summary>
        /// <param name="kit"></param>
        private void OnProcessRunning(SQLBuilder kit)
        {
            EventExecutor.InvokeExternalEvent(Sail, kit,
                EventFireTypeEnum.OnProcessRunning,
                base.ActivityResource.AppRunner.EventSubscriptionList,
                RunningActivityInstance.ProcessInstanceId.Value);
        }

        /// <summary>
        /// Process Complete event
        /// 执行流程完成事件
        /// </summary>
        /// <param name="kit"></param>
        private void OnProcessCompleted(SQLBuilder kit)
        {
            var pim = new ProcessInstanceManager(Sail);
            var entity = pim.GetById(kit, RunningActivityInstance.ProcessInstanceId.Value);
            if (entity.ProcessState == (short)ProcessStateEnum.Completed)
            {
                EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessCompleted,
                    base.ActivityResource.AppRunner.EventSubscriptionList,
                    RunningActivityInstance.ProcessInstanceId.Value);
            }
        }
    }
}
