using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using System;
using System.Text;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// Service node mediator
    /// ����ڵ�ִ����
    /// </summary>
    internal class NodeMediatorAIService : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorAIService(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        internal NodeMediatorAIService(SQLBuilder kit, SailClient client)
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
        internal override void ExecuteWorkItem(ActivityInstanceEntity currentActivityInstance)
        {
            try
            {
                //��ɽڵ��ϰ󶨵��ⲿ�����߼������
                //Complete the external program logic or service bound on the node
                OnExecutingAIServiceItem(base.ActivityForwardContext.FromActivityInstance, currentActivityInstance);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Completed automatically
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity currentActivity,
            ActivityInstanceEntity currentActivityInstance,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), currentActivityInstance, runner);
            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                currentActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
