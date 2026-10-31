
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.hub.Essential;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Message
{
    /// <summary>
    /// Intermediate Node Mediator Message Catch Continue
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterMsgCatchContinue : NodeMediator
    {
        internal NodeMediatorInterMsgCatchContinue(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.ActivityResource,
                    Kit);

                OnAfterExecuteWorkItem();

                //获取下一步节点列表：并继续执行
                //Get the next node list: and continue execution
                if (canContinueForwardCurrentNode)
                {
                    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Complete work item
        /// 完成节点实例
        /// </summary>
        /// <param name="activityResource"></param>
        /// <param name="kit"></param>        
        internal bool CompleteWorkItem(ActivityResource activityResource,
            SQLBuilder kit)
        {
            WfAppRunner runner = new WfAppRunner
            {
                UserId = activityResource.AppRunner.UserId,        
                UserName = activityResource.AppRunner.UserName
            };

            //设置活动节点的状态为完成状态
            //Set the status of the activity node to complete status
            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), LinkContext.FromActivityInstance.Id, activityResource.AppRunner);

            LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            ActivityForwardContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            bool canContinueForwardCurrentNode = LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
        }
    }
}
