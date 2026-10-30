
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Message;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Message
{
    /// <summary>
    /// Intermediate Node Mediator message catch
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterMsgCatch : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterMsgCatch(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Created automatically
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
        {
            var msgCatchActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);

            //写入默认第一次的预选步骤用户列表
            //Write the default user list for the first pre selection step
            msgCatchActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers);
            base.InsertActivityInstance(msgCatchActivityInstance, kit);

            return msgCatchActivityInstance;
        }

        /// <summary>
        /// Execute work item
        /// </summary>
        internal override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
        {
            try
            {
                OnBeforeExecuteWorkItem();

                OnAfterExecuteWorkItem();
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

            LinkContext.CurrentActivity = toActivity;
            LinkContext.CurrentActivityInstance = toActivityInstance;

            base.CreateActivityTaskTransitionInstance(toActivity,
                processInstance,
                fromActivityInstance,
                transitionId,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                ActivityForwardContext.ActivityResource,
                kit);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
    }
}
