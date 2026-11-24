using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.utils;
using sailflow.data;
using mooSQL.data;
using sailflow.xml;
using sailflow.signal;


using sailflow.hosting;
using sailflow.run;
namespace sailflow.run{
    /// <summary>
    /// 结束节点处理类
    /// </summary>
    internal class NodeMediatorEndSignalThrow : NodeMediator
    {
        internal NodeMediatorEndSignalThrow(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            OnBeforeExecuteWorkItem();

            var pim = Sail.ProcessInstances;
            var kit = Kit.useSQL();
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.ProcessInstanceId,
                ActivityForwardContext.ActivityResource.AppRunner);

            //在执行节点发布信号
            var msgDelegateService = new MessageDelegateService(Sail);
            msgDelegateService.PublishMessage(processInstance, LinkContext.CurrentActivity, ActivityForwardContext.FromActivityInstance);

            OnAfterExecuteWorkItem();
        }


        /// <summary>
        /// 结束节点活动及转移实例化，没有任务数据
        /// </summary>
        public override void CreateActivityTaskTransitionInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            string transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var toActivityInstance = CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);

            IActivityInstanceManager.Insert(kit.useSQL(), toActivityInstance);

            IActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance.ActivityInstanceId, activityResource.AppRunner);

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
