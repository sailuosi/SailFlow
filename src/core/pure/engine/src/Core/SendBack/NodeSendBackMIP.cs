using saiflow.engine.Client;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// The current node is processing returns in parallel signing mode
    /// 当前节点是并行会签模式下的退回处理
    /// </summary>
    internal class NodeSendBackMIP : NodeSendBack
    {
        internal NodeSendBackMIP(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
            : base(sendbackOperation, kit, client)
        {

        }

        /// <summary>
        /// Execute method
        /// </summary>
        internal override void Execute()
        {
            var runner = base.SendBackOperation.ActivityResource.AppRunner;
            var runningNode = base.SendBackOperation.BackwardFromActivityInstance;
            var previousActivityInstance = base.ActivityInstanceManager.GetPreviousActivityInstanceSimple(Kit.useSQL(), runningNode, base.SendBackOperation.BackwardToTaskActivity.ActivityId);

            //只退回到最后一个实例子节点的办理位置
            //Only return to the processing location of the last example node
            CreateBackwardActivityTaskTransitionOfLastMultipleInstance(base.SendBackOperation.ProcessInstance,
                base.SendBackOperation.BackwardFromActivityInstance,
                previousActivityInstance,
                base.SendBackOperation.BackwardType,
                TransitionTypeEnum.Sendback,
                TransitionFlyingTypeEnum.NotFlying,
                base.SendBackOperation.ActivityResource,
                base.Kit);
        }

        /// <summary>
        /// The revocation operation of the last multi instance node with multiple signatures
        /// 最后一个会签多实例子节点的撤销操作
        /// </summary>
        internal void CreateBackwardActivityTaskTransitionOfLastMultipleInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity originalBackwardToActivityInstance,
            BackwardTypeEnum backwardType,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var rollbackPreviousActivityInstance = base.ActivityInstanceManager.CreateBackwardActivityInstanceObject(processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.Id,
                processInstance.ProcessId,
                this.SendBackOperation.BackwardToTaskActivity,
                backwardType,
                fromActivityInstance.Id,
                originalBackwardToActivityInstance.Id,
                activityResource.AppRunner);

            rollbackPreviousActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            rollbackPreviousActivityInstance.MainActivityInstanceId = originalBackwardToActivityInstance.MainActivityInstanceId;
            rollbackPreviousActivityInstance.CompleteOrder = originalBackwardToActivityInstance.CompleteOrder;
            rollbackPreviousActivityInstance.ComplexType = originalBackwardToActivityInstance.ComplexType;
            rollbackPreviousActivityInstance.SignForwardType = originalBackwardToActivityInstance.SignForwardType;
            //人员来自步骤列表的用户数据
            //User data of performer from the step list
            rollbackPreviousActivityInstance.AssignedUserIds = base.SendBackOperation.BackwardToTaskPerformer.UserId;      
            rollbackPreviousActivityInstance.AssignedUserNames = base.SendBackOperation.BackwardToTaskPerformer.UserName;

            base.ActivityInstanceManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance);

            base.TaskManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance, base.SendBackOperation.BackwardToTaskPerformer, activityResource.AppRunner);

            base.InsertTransitionInstance(processInstance,
                fromActivityInstance,
                rollbackPreviousActivityInstance,
                TransitionTypeEnum.Backward,
                TransitionFlyingTypeEnum.NotFlying,
                this.SendBackOperation.ActivityResource.AppRunner,
                kit);
        }
    }
}
