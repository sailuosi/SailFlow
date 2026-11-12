
using System.Linq;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Result;
using sailflow.engine.Pattern;
using sailflow.engine.Core;

using sailflow.engine.Factories;
namespace sailflow.engine.Runtime
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

                NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(runningExecutionContext, kit);
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
