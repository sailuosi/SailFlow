
using System.Linq;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Handling of resend methods
    /// 返送回的处理
    /// </summary>
    internal class WfRuntimeManagerResend : WfRuntimeManager
    {
        /// <summary>
        /// Resend execution method
        /// 返送操作的处理逻辑
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

                NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(runningExecutionContext, kit, Sail);
                mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
                var toActivityId = runningExecutionContext.ActivityResource.NextActivityPerformers.First().Key;
                mediator.LinkContext.CurrentActivity = ProcessModel.GetActivity(toActivityId);
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
