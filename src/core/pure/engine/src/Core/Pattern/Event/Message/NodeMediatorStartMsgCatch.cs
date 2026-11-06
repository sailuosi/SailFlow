using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 开始节点执行器
    /// </summary>
    internal class NodeMediatorStartMsgCatch : NodeMediator
    {
        internal NodeMediatorStartMsgCatch(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                var pim = Sail.ProcessInstances;
                var kit = SailSqlAccess.Sibling(Kit);
                var newId = pim.Insert(kit, ActivityForwardContext.ProcessInstance);
                ActivityForwardContext.ProcessInstance.Id = newId;

                OnBeforeExecuteWorkItem();

                CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                    ActivityForwardContext.ActivityResource,
                    Kit);

                OnAfterExecuteWorkItem();

                //执行开始节点后的后续节点集合
                ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
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

            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), fromActivityInstance.Id.Value, activityResource.AppRunner);

            fromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            LinkContext.FromActivityInstance = fromActivityInstance;

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
