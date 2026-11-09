using saiflow.engine.Client;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// The predecessor node of the current running node is the parallel sign together node for the sendback processing
    /// 当前运行节点的前置节点是并行会签节点的退回处理
    /// </summary>
    internal class NodeSendBackToMIPPrevious : NodeSendBack
    {
        internal NodeSendBackToMIPPrevious(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
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

            //更新主节点状态从完成状态->挂起状态
            //Update the status of the main node from completed state to suspended state
            base.ActivityInstanceManager.Resuspend(Kit.useSQL(), previousActivityInstance.MainActivityInstanceId, base.SendBackOperation.ActivityResource.AppRunner);
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
            //创建回滚到的节点信息
            //Create rollback to node information
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
