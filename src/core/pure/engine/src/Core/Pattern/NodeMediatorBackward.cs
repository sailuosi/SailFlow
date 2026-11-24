using System;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.run;

namespace sailflow.run{
    /// <summary>
    /// 退回处理时的节点调节器（Reject / Reverse）。
    /// 会签/多实例 SendBack 已迁至 <c>NodeSendBack*</c>，勿在此恢复 MI 创建方法。
    /// </summary>
    internal class NodeMediatorBackward : NodeMediator
    {
        internal NodeMediatorBackward(BackwardContext backwardContext, SQLBuilder kit, SailClient client)
            : base(backwardContext, kit, client)
        {
        }

        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
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
                fromActivityInstance.ActivityInstanceId,
                base.BackwardContext.BackwardToTaskActivityInstance.ActivityInstanceId,
                activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Ready;
            toActivityInstance = GenerateActivitySignInfo(toActivityInstance, activityResource);

            base.IActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            base.ReturnDataContext.ActivityInstanceId = toActivityInstance.ActivityInstanceId;
            base.ReturnDataContext.ProcessInstanceId = toActivityInstance.ProcessInstanceId;

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
    }
}
