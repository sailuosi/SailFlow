
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Localize;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Xpdl;
using sailflow.engine.Runtime;
using Microsoft.VisualBasic;

using sailflow.engine.Client;
using sailflow.engine.Core;
namespace sailflow.engine.Pattern
{
    /// <summary>
    /// 中间事件节点(条件)处理类
    /// </summary>
    internal class NodeMediatorInterConditional : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterConditional(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 自动创建
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, FlowCommandContext runner, SQLBuilder kit)
        {
            var condActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);
            //保存下一步办理人信息
            condActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers, Sail.JsonSerializer);
            condActivityInstance.AssignedUserIds = WfDefine.SYSTEM_JOBTIMER_USER_ID;
            condActivityInstance.AssignedUserNames = WfDefine.SYSTEM_JOBTIMER_USER_NAME;

            //作业定时器信息
            condActivityInstance.JobTimerType = (short)JobTimerTypeEnum.Conditional;
            condActivityInstance.JobTimerStatus = (short)JobTimerStatusEnum.Ready;
            condActivityInstance.TriggerExpression = toActivity.TriggerDetail.Expression;

            base.InsertActivityInstance(condActivityInstance,
                kit);

            return condActivityInstance;
        }

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

        #region ICompleteAutomaticlly Member
        /// <summary>
        /// 自动完成
        /// </summary>
        public NodeAutoExecutedResult CompleteAutomaticlly(ProcessInstanceEntity processInstance,
            string transitionId,
            Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            Activity toActivity,
            ActivityInstanceEntity toActivityInstance,
            FlowCommandContext runner,
            SQLBuilder kit)
        {
            base.ActivityInstanceManager.Complete(kit.useSQL(), toActivityInstance, runner);
            LinkContext.CurrentActivityInstance = toActivityInstance;

            //写入转移实例数据
            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            //插入任务数据
            var tm = Sail.ManagerFactory.Tasks;
            var newTaskId = tm.Insert(kit.useSQL(), toActivityInstance,
                new Performer(WfDefine.SYSTEM_JOBTIMER_USER_ID, WfDefine.SYSTEM_JOBTIMER_USER_NAME),
                runner);
            tm.SetTaskType(kit.useSQL(), newTaskId, TaskTypeEnum.Automantic);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
        #endregion
    }
}
