using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Business.Entity;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// Service node mediator
    /// 任务节点执行器
    /// </summary>
    internal class NodeMediatorService : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorService(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorService(SQLBuilder kit, SailClient client)
            : base(kit, client)
        {

        }

        /// <summary>
        /// Created automatically
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
        {
            var aiServiceActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);
            base.InsertActivityInstance(aiServiceActivityInstance,
                kit);
            return aiServiceActivityInstance;
        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                //完成节点上绑定的外部程序逻辑或服务
                //Complete the external program logic or service bound on the node
                OnExecutingServiceItem();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Complete automatically
        /// </summary>
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

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
