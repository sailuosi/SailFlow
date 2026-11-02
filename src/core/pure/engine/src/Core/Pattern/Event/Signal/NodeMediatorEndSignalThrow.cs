using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Message;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Signal
{
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
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            OnBeforeExecuteWorkItem();

            ProcessInstanceManager pim = new ProcessInstanceManager(Sail);
            var kit = SailSqlAccess.Sibling(Kit);
            var processInstance = pim.Complete(kit, ActivityForwardContext.ProcessInstance.Id.Value,
                ActivityForwardContext.ActivityResource.AppRunner);

            //在执行节点发布信号
            var msgDelegateService = new MessageDelegateService(Sail);
            msgDelegateService.PublishMessage(processInstance, LinkContext.CurrentActivity, ActivityForwardContext.FromActivityInstance);

            OnAfterExecuteWorkItem();
        }


        /// <summary>
        /// 结束节点活动及转移实例化，没有任务数据
        /// </summary>
        internal override void CreateActivityTaskTransitionInstance(Activity toActivity,
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

            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);

            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), toActivityInstance.Id.Value, activityResource.AppRunner);

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
