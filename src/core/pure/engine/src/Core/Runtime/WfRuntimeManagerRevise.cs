using saiflow.data;
using mooSQL.data;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Handling of revise methods
    /// 流程重新修订处理类
    /// </summary>
    internal class WfRuntimeManagerRevise : WfRuntimeManager
    {
        /// <summary>
        /// Revise execute method
        /// 修订操作的处理逻辑
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            try
            {
                var runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                    base.ProcessModel,
                    base.ActivityResource,
                    true,
                    kit, Sail);

                NodeMediator mediator = new NodeMediatorRevise(runningExecutionContext, kit, Sail);
                mediator.ExecuteWorkItem(null);

                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Success;
                result.Message = mediator.GetNodeMediatedMessage();
            }
            catch (WfRuntimeException rx)
            {
                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.RunApp_RuntimeError;
                result.Message = rx.Message;
                throw;
            }
            catch (System.Exception ex)
            {
                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.RunApp_RuntimeError;
                result.Message = ex.Message;
                throw;
            }
        }
    }
}
