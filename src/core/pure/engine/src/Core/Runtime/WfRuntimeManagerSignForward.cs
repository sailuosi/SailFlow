using saiflow.data;
using mooSQL.data;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;
using saiflow.engine.Factories;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Runtime Manager Signforward
    /// 加签执行运行时
    /// </summary>
    internal class WfRuntimeManagerSignForward : WfRuntimeManager
    {
        /// <summary>
        /// The processing logic of signforward operation
        /// 加签执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            //根据加签类型选项，生成新的ActivityInstance记录
            //加签类型有前加签，后加签和并加签
            //Generate a new ActiveInstance record based on the signature type option
            //The types of signatures include before signature, behind signature, and parallel signature
            try
            {
                var signforwardExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                    base.ProcessModel,
                    base.ActivityResource,
                    false,
                    kit, Sail);

                NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(signforwardExecutionContext, kit);
                mediator.ExecuteWorkItem(null);

                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Success;
                result.Message = mediator.GetNodeMediatedMessage();
            }
            catch (WfRuntimeException rx)
            {
                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.SignForward_RuntimeError;
                result.Message = rx.Message;
                throw;
            }
            catch (System.Exception ex)
            {
                var result = base.WfExecutedResult;
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.SignForward_RuntimeError;
                result.Message = ex.Message;
                throw;
            }
        }
    }
}
