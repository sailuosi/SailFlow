using System.Linq;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Manager;
using saiflow.engine.Common;
using saiflow.engine.Result;
using saiflow.engine.Pattern;
using saiflow.engine.Core;

using saiflow.engine.Factories;
namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Handling of jump methods
    /// 跳转方式的处理
    /// </summary>
    internal class WfRuntimeManagerJump : WfRuntimeManager
    {
        /// <summary>
        /// Jump execution method
        /// 跳转执行方法
        /// </summary>
        /// <param name="kit"></param>
        internal override void ExecuteInstanceImp(SQLBuilder kit)
        {
            WfExecutedResult result = base.WfExecutedResult;

            var aim = Sail.ManagerFactory.Activities;
            aim.Complete(SailSqlAccess.Sibling(kit), base.RunningActivityInstance.Id.Value, this.AppRunner);

            //构建跳转上下文
            //Build jump context
            var jumpActivityId = base.AppRunner.NextActivityPerformers.First().Key;
            var jumpforwardActivity = base.ProcessModel.GetActivity(jumpActivityId);
            var processInstance = Sail.ProcessInstances.GetById(base.RunningActivityInstance.ProcessInstanceId.Value);

            var jumpforwardExecutionContext = ActivityForwardContext.CreateRunningContextByTask(base.TaskView,
                    base.ProcessModel,
                    base.ActivityResource,
                    true,
                    kit, Sail);
            jumpforwardExecutionContext.TaskId = this.TaskView.Id;
            jumpforwardExecutionContext.FromActivityInstance = base.RunningActivityInstance;

            NodeMediator mediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediator(jumpforwardExecutionContext, kit);
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
