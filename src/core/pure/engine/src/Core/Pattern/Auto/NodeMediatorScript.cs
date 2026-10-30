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
    /// Script node mediator
    /// ����ڵ�ִ����
    /// </summary>
    internal class NodeMediatorScript : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorScript(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorScript(SQLBuilder kit, SailClient client)
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
                //��ɽڵ��ϰ󶨵��ⲿ�����߼������
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
