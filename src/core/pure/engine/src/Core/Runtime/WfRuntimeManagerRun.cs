using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Core.Pattern;
using saiflow.engine.Core.Result;
using saiflow.engine.Event;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;

namespace saiflow.engine.Core.Runtime
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
                    //有TaskID的任务类型执行上下文
                    //Task type execution context with TaskId
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit);
                }
                else
                {
                    //Interrupt事件类型的活动执行上下文
                    //Interrupt event type activity execution context
                    runningExecutionContext = ActivityForwardContext.CreateRunningContextByActivity(base.RunningActivityInstance,
                        base.ProcessModel,
                        base.ActivityResource,
                        false,
                        kit);
                }

                OnProcessRunning(kit);

                NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(runningExecutionContext, kit);
                mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
                mediator.ExecuteWorkItem(null);

                OnProcessCompleted(kit);
               
                result.Status = WfExecutedStatus.Success;
                result.Message = mediator.GetNodeMediatedMessage();
            }
            catch (WfRuntimeException rx)
            {
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.RunApp_RuntimeError;
                result.Message = rx.Message;
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
            EventExecutor.InvokeExternalEvent(kit,
                EventFireTypeEnum.OnProcessRunning,
                base.ActivityResource.AppRunner.EventSubscriptionList,
                RunningActivityInstance.ProcessInstanceId);
        }

        /// <summary>
        /// Process Complete event
        /// 执行流程完成事件
        /// </summary>
        /// <param name="kit"></param>
        private void OnProcessCompleted(SQLBuilder kit)
        {
            var pim = new ProcessInstanceManager();
            var entity = pim.GetById(kit, RunningActivityInstance.ProcessInstanceId);
            if (entity.ProcessState == (short)ProcessStateEnum.Completed)
            {
                EventExecutor.InvokeExternalEvent(kit,
                    EventFireTypeEnum.OnProcessCompleted,
                    base.ActivityResource.AppRunner.EventSubscriptionList,
                    RunningActivityInstance.ProcessInstanceId);
            }
        }
    }
}
