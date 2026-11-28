
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.utils;
using sailflow.data;
using mooSQL.data;
using sailflow.xml;
using sailflow.signal;

using sailflow.hosting;
using sailflow.run;
namespace sailflow.run{
    /// <summary>
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterMsgCatch : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterMsgCatch(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 自动创建
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, FlowCommandContext context, SQLBuilder kit)
        {
            var msgCatchActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, context);

            //写入首步预选默认用户列表
            msgCatchActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(context.NextActivityPerformers, Sail.JsonSerializer);
            base.InsertActivityInstance(msgCatchActivityInstance, kit);

            return msgCatchActivityInstance;
        }

        /// <summary>
        /// 执行工作项
        /// </summary>
        public override void ExecuteWorkItem(ActivityInstanceEntity activityInstance)
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
        /// 自动完成
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity toActivity,
            ActivityInstanceEntity toActivityInstance,
            FlowCommandContext context,
            SQLBuilder kit)
        {
            base.IActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance, context);

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
