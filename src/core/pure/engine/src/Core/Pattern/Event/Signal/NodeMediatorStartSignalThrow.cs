
using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Message;
using System.CodeDom.Compiler;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Signal
{
    /// <summary>
    /// Start signal throw node mediator
    /// 开始节点执行器
    /// </summary>
    internal class NodeMediatorStartSignalThrow : NodeMediator
    {
        internal NodeMediatorStartSignalThrow(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                ProcessInstanceManager pim = new ProcessInstanceManager(Sail);
                var kit = SailSqlAccess.Sibling(Kit);
                var newId = pim.Insert(kit, ActivityForwardContext.ProcessInstance);
                ActivityForwardContext.ProcessInstance.Id = newId;

                OnBeforeExecuteWorkItem();

                CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                    ActivityForwardContext.ActivityResource,
                    Kit);

                OnAfterExecuteWorkItem();

                //执行开始节点之后的节点集合
                //Collection of nodes after executing the start node
                ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Complete automatically
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="activityResource"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        private NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var fromActivityInstance = CreateActivityInstanceObject(LinkContext.FromActivity, processInstance, activityResource.AppRunner);

            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), fromActivityInstance);

            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), fromActivityInstance.Id, activityResource.AppRunner);

            fromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            LinkContext.FromActivityInstance = fromActivityInstance;

            //执行节点上的信号发布
            //Signal release on the execution node
            var signalDelegateService = new SignalDelegateService(Sail);
            signalDelegateService.PublishSignal(processInstance, LinkContext.FromActivity, fromActivityInstance);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
