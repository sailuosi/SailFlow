using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Runtime;
using saiflow.engine.Message;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterTimer : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterTimer(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// 自动创建
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, FlowCommandContext runner, SQLBuilder kit)
        {
            var timerActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);

            //保存下一步人员信息
            timerActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers, Sail.JsonSerializer);

            //逾期时间
            timerActivityInstance.OverdueDateTime = CalcDataTimeFromVariable(toActivity);
            timerActivityInstance.AssignedUserIds = WfDefine.SYSTEM_JOBTIMER_USER_ID;
            timerActivityInstance.AssignedUserNames = WfDefine.SYSTEM_JOBTIMER_USER_NAME;

            //作业定时器
            timerActivityInstance.JobTimerType = (short)JobTimerTypeEnum.Timer;
            timerActivityInstance.JobTimerStatus = (short)JobTimerStatusEnum.Ready;

            base.InsertActivityInstance(timerActivityInstance,
                kit);

            return timerActivityInstance;
        }

        /// <summary>
        /// 执行工作项
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

            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            var tm = Sail.ManagerFactory.Tasks;
            var newTaskId = tm.Insert(kit.useSQL(), toActivityInstance,
                new Performer(WfDefine.SYSTEM_JOBTIMER_USER_ID, WfDefine.SYSTEM_JOBTIMER_USER_NAME),
                runner);
            tm.SetTaskType(kit.useSQL(), newTaskId, TaskTypeEnum.Automantic);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }

        /// <summary>
        /// 计算延迟时间
        /// </summary>
        /// <param name="timerActivity"></param>
        /// <returns></returns>
        private DateTime CalcDataTimeFromVariable(Activity timerActivity)
        {
            if (!string.IsNullOrEmpty(timerActivity.TriggerDetail.Expression))
            {
                var expression = timerActivity.TriggerDetail.Expression;
                try
                {
                    var timeSpan = System.Xml.XmlConvert.ToTimeSpan(expression);
                    var overdueDateTime = DateTime.UtcNow.Add(timeSpan);
                    return overdueDateTime;
                }
                catch (Exception ex)
                {
                    throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediatorintertimer.CalcDataTimeFromVariable.error", expression), ex);
                }
            }
            else
            {
                throw new WfXpdlException(Sail.Messages.GetEngineMessage("nodemediatorintertimer.CalcDataTimeFromVariable.error"));
            }
        }
    }
}
