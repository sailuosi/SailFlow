
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
namespace saiflow.engine.Core.Pattern.Event.Signal
{
    /// <summary>
    /// Intermediate Signal throw node mediator
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterSignalThrow : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterSignalThrow(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Created automatically
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
        {
            var messageActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);
            base.InsertActivityInstance(messageActivityInstance,
                kit);
            return messageActivityInstance;
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

            //执行节点上的信号发布
            //Signal release on the execution node
            var signalDelegateService = new SignalDelegateService(Sail);
            signalDelegateService.PublishSignal(processInstance, LinkContext.CurrentActivity, toActivityInstance);

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
