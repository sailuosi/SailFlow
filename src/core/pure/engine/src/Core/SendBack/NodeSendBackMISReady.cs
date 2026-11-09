using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Sendback processor for task types
    /// Sequence signature, the first node is in pending status
    /// 任务类型的退回处理器
    /// 串行会签，第一个节点处于待办
    /// </summary>
    internal class NodeSendBackMISReady : NodeSendBack
    {
        internal NodeSendBackMISReady(SendBackOperation sendbackOperation, SQLBuilder kit, SailClient client)
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
            var runningMainNode = base.ActivityInstanceManager.GetById(runningNode.MainActivityInstanceId);
            var previousActivityInstance = base.ActivityInstanceManager.GetPreviousActivityInstanceSimple(Kit.useSQL(), runningNode, base.SendBackOperation.BackwardToTaskActivity.ActivityId);

            //创建撤销到上一步的节点记录
            //Create a node record to sendback to the previous step
            CreateBackwardActivityTaskTransitionOfLastMultipleInstance(base.SendBackOperation.ProcessInstance,
                runningMainNode,
                previousActivityInstance,
                base.SendBackOperation.BackwardType,
                TransitionTypeEnum.Sendback,
                TransitionFlyingTypeEnum.NotFlying,
                base.SendBackOperation.ActivityResource,
                base.Kit);

            //如果上一步节点也是会签节点，更新主节点状态为运行状态
            //If the previous node is also a co signing node, update the main node status to running status
            if (previousActivityInstance.ActivityType == (short)ActivityTypeEnum.MultiSignNode)
            {
                base.ActivityInstanceManager.Rerun(Kit.useSQL(), previousActivityInstance.MainActivityInstanceId, runner);
            }
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
            var rollbackPreviousActivityInstance = base.CreateBackwardToActivityInstanceObject(processInstance,
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
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);
        }
    }
}
