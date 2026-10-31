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
using saiflow.engine.Message;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Pattern.Event.Timer
{
    /// <summary>
    /// Intermediate timer node mediator
    /// 中间事件节点处理类
    /// </summary>
    internal class NodeMediatorInterTimer : NodeMediator, ICreatedAutomaticlly, ICompletedAutomaticlly
    {
        internal NodeMediatorInterTimer(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
            : base(forwardContext, kit, client)
        {

        }

        /// <summary>
        /// Created automatically
        /// </summary>
        public ActivityInstanceEntity CreatedAutomaticlly(Activity toActivity, ProcessInstanceEntity processInstance, WfAppRunner runner, SQLBuilder kit)
        {
            var timerActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);

            //存储下一步步骤人员信息
            //Store personnel information for the next step
            timerActivityInstance.NextStepPerformers = NextStepUtility.SerializeNextStepPerformers(runner.NextActivityPerformers, Sail.JsonSerializer);

            //延时时间信息
            //Overdue datetime
            timerActivityInstance.OverdueDateTime = CalcDataTimeFromVariable(toActivity);
            timerActivityInstance.AssignedUserIds = WfDefine.SYSTEM_JOBTIMER_USER_ID;
            timerActivityInstance.AssignedUserNames = WfDefine.SYSTEM_JOBTIMER_USER_NAME;

            //定时作业信息
            //job timer
            timerActivityInstance.JobTimerType = (short)JobTimerTypeEnum.Timer;
            timerActivityInstance.JobTimerStatus = (short)JobTimerStatusEnum.Ready;

            base.InsertActivityInstance(timerActivityInstance,
                kit);

            return timerActivityInstance;
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

            LinkContext.CurrentActivityInstance = toActivityInstance;

            base.InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                TransitionTypeEnum.Forward,
                TransitionFlyingTypeEnum.NotFlying,
                runner,
                kit);

            var tm = new TaskManager(Sail);
            var newTaskId = tm.Insert(SailSqlAccess.Sibling(kit), toActivityInstance,
                new Performer(WfDefine.SYSTEM_JOBTIMER_USER_ID, WfDefine.SYSTEM_JOBTIMER_USER_NAME),
                runner);
            tm.SetTaskType(SailSqlAccess.Sibling(kit), newTaskId, TaskTypeEnum.Automantic);

            NodeAutoExecutedResult result = NodeAutoExecutedResult.CreateGatewayExecutedResult(NodeAutoExecutedStatus.Successed);
            return result;
        }

        /// <summary>
        /// Calculate delay time
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
