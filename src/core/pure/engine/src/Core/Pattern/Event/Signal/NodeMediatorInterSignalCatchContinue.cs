
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;
using saiflow.hub.Essential;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterSignalCatchContinue : NodeMediator
    {
        internal NodeMediatorInterSignalCatchContinue(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
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
                OnBeforeExecuteWorkItem();

                bool canContinueForwardCurrentNode = CompleteWorkItem(ActivityForwardContext.ActivityResource,
                    Kit);

                OnAfterExecuteWorkItem();

                //获取下一步节点列表：并继续执行
                //获取下一节点列表并继续执行
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

            ActivityInstanceManager.Complete(kit.useSQL(), LinkContext.FromActivityInstance.Id.Value, activityResource.AppRunner);

            LinkContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            ActivityForwardContext.FromActivityInstance.ActivityState = (short)ActivityStateEnum.Completed;
            bool canContinueForwardCurrentNode = LinkContext.FromActivityInstance.CanNotRenewInstance == 0;

            return canContinueForwardCurrentNode;
        }
    }
}
