using sailflow.hosting;
using sailflow.data;
using mooSQL.data;
using sailflow.utils;


namespace sailflow.run{
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
        public override void Execute()
        {
            var context = base.SendBackOperation.ActivityResource.AppRunner;
            var runningNode = base.SendBackOperation.BackwardFromActivityInstance;
            var previousActivityInstance = base.IActivityInstanceManager.GetPreviousActivityInstanceSimple(Kit.useSQL(), runningNode, base.SendBackOperation.BackwardToTaskActivity.ActivityId);

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
            base.IActivityInstanceManager.Resuspend(Kit.useSQL(), previousActivityInstance.MainActInstanceId, base.SendBackOperation.ActivityResource.AppRunner);
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
            var rollbackPreviousActivityInstance = base.IActivityInstanceManager.CreateBackwardActivityInstanceObject(processInstance.BizChannel,
                processInstance.BizPK,
                processInstance.BizCode,
                processInstance.ProcessInstanceId,
                processInstance.ProcessId,
                this.SendBackOperation.BackwardToTaskActivity,
                backwardType,
                fromActivityInstance.ActivityInstanceId,
                originalBackwardToActivityInstance.ActivityInstanceId,
                activityResource.AppRunner);

            rollbackPreviousActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            rollbackPreviousActivityInstance.MainActInstanceId = originalBackwardToActivityInstance.MainActInstanceId;
            rollbackPreviousActivityInstance.CompleteOrder = originalBackwardToActivityInstance.CompleteOrder;
            rollbackPreviousActivityInstance.ComplexType = originalBackwardToActivityInstance.ComplexType;
            rollbackPreviousActivityInstance.SignForwardType = originalBackwardToActivityInstance.SignForwardType;
            //人员来自步骤列表的用户数据
            //User data of performer from the step list
            rollbackPreviousActivityInstance.SignIds = base.SendBackOperation.BackwardToTaskPerformer.UserId;      
            rollbackPreviousActivityInstance.SignNames = base.SendBackOperation.BackwardToTaskPerformer.UserName;

            base.IActivityInstanceManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance);

            base.ITaskManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance, base.SendBackOperation.BackwardToTaskPerformer, activityResource.AppRunner);

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
