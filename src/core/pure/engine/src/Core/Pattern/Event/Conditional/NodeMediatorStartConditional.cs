using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Core.Runtime;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;


using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Conditional
{
    /// <summary>
    /// Node Mediator Start Conditional
    /// 开始节点执行器
    /// </summary>
    internal class NodeMediatorStartConditional : NodeMediator
    {
        internal NodeMediatorStartConditional(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Execute Work Item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                //判断条件表达式是否满足
                //Determine whether the conditional expression satisfies
                var expression = ActivityForwardContext.CurrentActivity.TriggerDetail.Expression;
                var dvKeyValuePair = ActivityForwardContext.ActivityResource.AppRunner.Conditions;

                string expressionReplaced = ExpressionParser.ReplaceParameterToValue(expression, dvKeyValuePair);
                var result = ExpressionParser.Parse(expressionReplaced);

                if (result == true)
                {
                    ProcessInstanceManager pim = new ProcessInstanceManager(Sail);
                    var kit = SailSqlAccess.Sibling(Kit);
                    var newId = pim.Insert(kit, ActivityForwardContext.ProcessInstance);
                    ActivityForwardContext.ProcessInstance.Id = newId;

                    //执行前Action列表
                    //On before execute work item
                    OnBeforeExecuteWorkItem();

                    CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                        ActivityForwardContext.ActivityResource,
                        Kit);

                    //执行后Action列表
                    //ON after execute work item
                    OnAfterExecuteWorkItem();

                    //执行开始节点之后的节点集合
                    //Collection of nodes after executing the start node
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
                }
                else
                {
                    throw new WfRuntimeException(LocalizeMessagesLocator.Current.GetEngineMessage("nodemediatorstartconditional.ExecuteWorkItem.exception"));
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Set the start node to the completed state
        /// 置开始节点为结束状态
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

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
