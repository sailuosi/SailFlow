
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Core.Runtime;
using Microsoft.VisualBasic;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Conditional
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
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
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
            WfAppRunner runner,
            SQLBuilder kit)
        {
            base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), toActivityInstance, runner);
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
            var tm = new TaskManager(Sail);
            var newTaskId = tm.Insert(SailSqlAccess.Sibling(kit), toActivityInstance,
                new Performer(WfDefine.SYSTEM_JOBTIMER_USER_ID, WfDefine.SYSTEM_JOBTIMER_USER_NAME),
                runner);
            tm.SetTaskType(SailSqlAccess.Sibling(kit), newTaskId, TaskTypeEnum.Automantic);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }
        #endregion
    }
}
