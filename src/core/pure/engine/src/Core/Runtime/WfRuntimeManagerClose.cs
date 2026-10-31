using System.Linq;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Manager;
using saiflow.engine.Common;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Pattern;
using saiflow.engine.Core.Pattern.Event;

namespace saiflow.engine.Core.Runtime
{
    /// <summary>
    /// Workflow Runtimer Manager Close
    /// 流程运行时关闭
    /// </summary>
    internal class WfRuntimeManagerClose : WfRuntimeManager
    {
        /// <summary>
        /// Execution method for closing operation
        /// 关闭操作的执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            WfExecutedResult result = base.WfExecutedResult;

            //设置当前活动实例为完成状态
            //Set the current activity instance to completion status
            var aim = new ActivityInstanceManager(Sail);
            aim.Complete(SailSqlAccess.Sibling(kit), base.RunningActivityInstance.Id.Value, this.AppRunner);

            var jumpActivityId = base.AppRunner.NextActivityPerformers.First().Key;
            var jumpforwardActivity = base.ProcessModel.GetActivity(jumpActivityId);
            var processInstance = (new ProcessInstanceManager(Sail)).GetById(base.RunningActivityInstance.ProcessInstanceId.Value);
            var jumpforwardExecutionContext = ActivityForwardContext.CreateJumpforwardContext(jumpforwardActivity,
                base.ProcessModel, processInstance, base.ActivityResource);
            jumpforwardExecutionContext.FromActivityInstance = base.RunningActivityInstance;

            NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(jumpforwardExecutionContext, kit, Sail);
            mediator.LinkContext.FromActivityInstance = base.RunningActivityInstance;
            mediator.LinkContext.CurrentActivity = jumpforwardActivity;

            if (mediator is NodeMediatorEnd)
            {
                //结束节点的连线转移
                //Insert transition instance of End activity
                mediator.CreateActivityTaskTransitionInstance(jumpforwardActivity,
                    processInstance,
                    base.RunningActivityInstance,
                    WfDefine.WF_XPDL_JUMP_BYPASS_GUID,
                    TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.ForwardFlying,
                    base.ActivityResource,
                    kit);
            }
            mediator.ExecuteWorkItem(null);

            result.Status = WfExecutedStatus.Success;
            result.Message = mediator.GetNodeMediatedMessage();
        }
    }
}
