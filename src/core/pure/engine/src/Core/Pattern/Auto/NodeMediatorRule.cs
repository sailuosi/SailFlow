using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// 业务规则任务执行器
    /// </summary>
    internal class NodeMediatorRule : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorRule(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {
        }

        internal NodeMediatorRule(SQLBuilder kit, SailClient client)
            : base(kit, client)
        {
        }

        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
        {
            var activityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);
            base.InsertActivityInstance(activityInstance, kit);
            return activityInstance;
        }

        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            var currentActivity = LinkContext.CurrentActivity;
            if (currentActivity == null
                || currentActivity.RuleConfigDetail == null
                || string.IsNullOrWhiteSpace(currentActivity.RuleConfigDetail.RuleSetCode))
            {
                throw new WorkflowException("RuleTask configuration is missing ruleSetCode.");
            }

            var eventService = GetInnerEventService();
            RuleExecutor.ExecuteRule(Sail, currentActivity.RuleConfigDetail, currentActivity, activityInstance, eventService);
        }

        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity toActivity,
            ActivityInstanceEntity toActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), toActivityInstance, runner);
            LinkContext.CurrentActivityInstance = toActivityInstance;
            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            return NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
        }
    }
}
