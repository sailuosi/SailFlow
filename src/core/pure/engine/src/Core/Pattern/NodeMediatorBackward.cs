
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;


using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 退回处理时的节点调节器
    /// </summary>
    internal class NodeMediatorBackward : NodeMediator
    {
        internal NodeMediatorBackward(BackwardContext backwardContext, SQLBuilder kit, SailClient client)
            : base(backwardContext, kit, client)
        {
            
        }

        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 创建退回时的流转节点对象、任务和转移数据
        /// </summary>
        internal void CreateBackwardActivityTaskTransitionInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            BackwardTypeEnum backwardType,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var toActivityInstance = base.CreateBackwardToActivityInstanceObject(processInstance,
                backwardType,
                fromActivityInstance.Id.Value,
                base.BackwardContext.BackwardToTaskActivityInstance.Id.Value,
                activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

            base.ActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            base.ReturnDataContext.ActivityInstanceId = toActivityInstance.Id.Value;
            base.ReturnDataContext.ProcessInstanceId = toActivityInstance.ProcessInstanceId.Value;

            base.CreateNewTask(toActivityInstance, activityResource, kit);

            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);
        }

        /// <summary>
        /// 退回是会签情况下的处理：
        /// 要退回的节点是会签节点
        /// 1) 全部实例化会签节点下的多实例节点
        /// 2) 只取得办理完成的节点，而且保证CompleteOrder的唯一性
        /// </summary>
        internal void CreateBackwardActivityTaskRepeatedSignTogetherMultipleInstance(ProcessInstanceEntity processInstance,
            Activity backwardToTaskActvity,
            ActivityInstanceEntity fromActivityInstance,
            BackwardTypeEnum backwardType,
            ActivityInstanceEntity previousMainInstance,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            //上一步节点是会签节点的退回处理
            //需要重新实例化会签节点上的所有办理人的任务
            //重新封装任务办理人为AssignedUsers, AssignedUsernames
            var performerList = AntiGenerateActivityPerformerList(previousMainInstance);

            activityResource.NextActivityPerformers.Clear();
            activityResource.NextActivityPerformers = new Dictionary<string, PerformerList>();
            activityResource.NextActivityPerformers.Add(backwardToTaskActvity.ActivityId, performerList);

            //重新生成会签节点多实例数据
            CreateMultipleInstance(backwardToTaskActvity, processInstance, fromActivityInstance,
                transitionId, transitionType, flyingType, activityResource, kit);
        }

        /// <summary>
        /// 创建多实例节点之间回滚时的活动实例，任务数据
        /// </summary>
        internal void CreateBackwardActivityTaskOfInnerMultipleInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity originalBackwardToActivityInstance,
            BackwardTypeEnum backwardType,
            int backSrcActivityInstanceId,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            //创建退回目标节点信息
            var rollbackPreviousActivityInstance = base.CreateBackwardToActivityInstanceObject(processInstance,
                backwardType,
                backSrcActivityInstanceId,
                originalBackwardToActivityInstance.Id.Value,
                activityResource.AppRunner);

            rollbackPreviousActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            rollbackPreviousActivityInstance.MainActivityInstanceId = originalBackwardToActivityInstance.MainActivityInstanceId;
            rollbackPreviousActivityInstance.CompleteOrder = originalBackwardToActivityInstance.CompleteOrder;
            rollbackPreviousActivityInstance.ComplexType = originalBackwardToActivityInstance.ComplexType;
            rollbackPreviousActivityInstance.SignForwardType = originalBackwardToActivityInstance.SignForwardType;
            rollbackPreviousActivityInstance.AssignedUserIds = originalBackwardToActivityInstance.AssignedUserIds;      //多实例节点为单一用户任务
            rollbackPreviousActivityInstance.AssignedUserNames = originalBackwardToActivityInstance.AssignedUserNames;

            base.ActivityInstanceManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance);

            base.ReturnDataContext.ActivityInstanceId = rollbackPreviousActivityInstance.Id.Value;
            base.ReturnDataContext.ProcessInstanceId = rollbackPreviousActivityInstance.ProcessInstanceId.Value;

            base.CreateNewTask(rollbackPreviousActivityInstance, activityResource, kit);
        }

        /// <summary>
        /// 最后一个会签多实例子节点的撤销操作
        /// </summary>
        internal void CreateBackwardActivityTaskTransitionOfLastMultipleInstance(ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity originalBackwardToActivityInstance,
            BackwardTypeEnum backwardType,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            //创建退回目标节点信息
            var rollbackPreviousActivityInstance = base.CreateBackwardToActivityInstanceObject(processInstance,
                backwardType,
                fromActivityInstance.Id.Value,
                originalBackwardToActivityInstance.Id.Value,
                activityResource.AppRunner);

            rollbackPreviousActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            rollbackPreviousActivityInstance.MainActivityInstanceId = originalBackwardToActivityInstance.MainActivityInstanceId;
            rollbackPreviousActivityInstance.CompleteOrder = originalBackwardToActivityInstance.CompleteOrder;
            rollbackPreviousActivityInstance.ComplexType = originalBackwardToActivityInstance.ComplexType;
            rollbackPreviousActivityInstance.SignForwardType = originalBackwardToActivityInstance.SignForwardType;
            rollbackPreviousActivityInstance.AssignedUserIds = originalBackwardToActivityInstance.AssignedUserIds;      //多实例节点为单一用户任务
            rollbackPreviousActivityInstance.AssignedUserNames = originalBackwardToActivityInstance.AssignedUserNames;

            base.ActivityInstanceManager.Insert(kit.useSQL(), rollbackPreviousActivityInstance);
            base.ReturnDataContext.ActivityInstanceId = rollbackPreviousActivityInstance.Id.Value;
            base.ReturnDataContext.ProcessInstanceId = rollbackPreviousActivityInstance.ProcessInstanceId.Value;
            base.CreateNewTask(rollbackPreviousActivityInstance, activityResource, kit);

            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                rollbackPreviousActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);
        }
    }
}
