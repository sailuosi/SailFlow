using sailflow.data;
using mooSQL.data;
using sailflow.engine.Manager;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;
using sailflow.hub.Localize;
using sailflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using sailflow.engine.Factories;
using sailflow.engine.NextStep;

namespace sailflow.engine.Xpdl
{
    internal class ProcessModelBPMNCore
    {
        /// <summary>
        /// Obtain the next node list accompanied by runtime condition information
        /// 获取下一步节点列表，伴随运行时条件信息
        /// </summary>
        internal NextActivityMatchedResult GetNextActivityTreeListCore(SailClient client, IProcessModel processModel,
            string currentActivityId,
            string activityInstanceId,
            IDictionary<string, string> conditionKeyValuePair,
            SQLBuilder kit)
        {
            try
            {
                NextActivityMatchedResult result = null;
                NextActivityMatchedType resultType = NextActivityMatchedType.Unknown;

                // Client 级 Prepare：写同一条件袋（预览 / 发送同源）
                var currentActivity = processModel?.GetActivity(currentActivityId);
                conditionKeyValuePair = NextStepRouteRuntime.ApplyPrepare(
                    client,
                    conditionKeyValuePair,
                    currentActivity,
                    processModel?.ProcessEntity,
                    string.IsNullOrEmpty(activityInstanceId)
                        ? NextStepRoutePhase.Preview
                        : NextStepRoutePhase.Send,
                    activityInstanceId: activityInstanceId);

                //创建“下一步节点”的根节点
                //Create the root node for the 'next step node'
                NextActivityComponent root = NextActivityComponents.CreateNextActivityComponent();

                //开始正常情况下的路径查找
                //Start normal path search
                List<Transition> transitionList = GetForwardTransitionList(client, processModel,
                    currentActivityId,
                    conditionKeyValuePair).ToList();

                if (transitionList.Count > 0)
                {
                    //遍历连线，获取下一步节点的列表
                    //Traverse the connection to obtain the list of next nodes
                    NextActivityComponent child = null;
                    foreach (Transition transition in transitionList)
                    {
                        // 残缺连线（无 targetRef / 目标活动不存在）：跳过，避免 ToActivity NRE
                        if (transition?.ToActivity == null)
                            continue;

                        if (XPDLHelper.IsSimpleComponentNode(transition.ToActivity.ActivityType) == true)
                        {
                            child = NextActivityComponents.CreateNextActivityComponent(transition, transition.ToActivity);
                        }
                        else if (XPDLHelper.IsGatewayComponentNode(transition.ToActivity.ActivityType) == true)
                        {
                            NextActivityScheduleBase activitySchedule = SailFactoryAccess.ProcessModels(client).CreateActivityScheduleGateway(processModel,
                                transition.ToActivity.GatewayDetail.SplitJoinType,
                                activityInstanceId);

                            //获取网关后面的节点
                            //Get the nodes behind the gateway
                            child = activitySchedule.GetNextActivityListFromGateway(transition,
                                transition.ToActivity,
                                conditionKeyValuePair,
                                kit,
                                out resultType);
                        }
                        else if (XPDLHelper.IsCrossOverComponentNode(transition.ToActivity.ActivityType) == true)
                        {
                            //事件类型的特殊节点处理，跟网关类似
                            //Special node handling for event types, similar to gateways
                            NextActivityScheduleBase activitySchedule = DefaultProcessModelFactory.CreateActivityScheduleIntermediate(processModel);
                            child = activitySchedule.GetNextActivityListFromGateway(transition,
                                transition.ToActivity,
                                conditionKeyValuePair,
                                kit,
                                out resultType);
                        }
                        else
                        {
                            var errMsg = string.Format("Unknown node type：{0}", transition.ToActivity.ActivityType.ToString());
                            LogManager.RecordLog(client, client.Messages.GetEngineMessage("processmodel.getnextactivitylist.error"),
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfXpdlException(errMsg));
                            throw new XmlDefinitionException(errMsg);
                        }

                        if (child != null)
                        {
                            root.Add(child);
                            resultType = NextActivityMatchedType.Successed;
                        }
                    }
                }
                else
                {
                    resultType = NextActivityMatchedType.NoneTransitionFilteredByCondition;
                }
                result = NextActivityMatchedResult.CreateNextActivityMatchedResultObject(resultType, root);
                return result;
            }
            catch (System.Exception e)
            {
                LogManager.RecordLog(client, client.Messages.GetEngineMessage("processmodel.getnextactivitylist.error"),
                    LogEventType.Exception,
                    LogPriority.Normal,
                    null,
                    new WfXpdlException(e.Message));
                throw new WfXpdlException(client.Messages.GetEngineMessage("processmodel.getnextactivitylist.error", e.Message),
                    e);
            }
        }

        private IList<Transition> GetForwardTransitionList(
            SailClient client,
            IProcessModel processModel,
            string fromActivityId,
            IDictionary<string, string> conditionKeyValuePair)
        {
            var validTransitionList = new List<Transition>();
            var transitionList = ProcessModelHelper.GetForwardTransitionList(processModel.Process, fromActivityId);
            foreach (var transition in transitionList)
            {
                bool isValidTranstion = IsValidTransition(client, processModel, transition, conditionKeyValuePair);
                if (isValidTranstion) validTransitionList.Add(transition);
            }
            return validTransitionList;
        }

        /// <summary>
        /// Is it a transition that meets the conditions? If the condition is empty, it defaults to being valid.
        /// </summary>
        private bool IsValidTransition(
            SailClient client,
            IProcessModel processModel,
            Transition transition,
            IDictionary<string, string> conditionKeyValuePair)
        {
            bool isValid = false;

            if (transition.Condition != null && !string.IsNullOrEmpty(transition.Condition.ConditionText))
            {
                if (conditionKeyValuePair != null && conditionKeyValuePair.Count != 0)
                {
                    isValid = ParseCondition(client, processModel, transition, conditionKeyValuePair);
                }
            }
            else
            {
                isValid = true;
            }
            return isValid;
        }

        private bool ParseCondition(
            SailClient client,
            IProcessModel processModel,
            Transition transition,
            IDictionary<string, string> conditionKeyValuePair)
        {
            var expression = transition.Condition.ConditionText;
            var pipeline = NextStepConditionRequire.GetPipelineRequired(client);
            var entity = processModel?.ProcessEntity;
            return pipeline.Evaluate(new ConditionEvaluateContext
            {
                Sail = client,
                Expression = expression,
                Variables = conditionKeyValuePair,
                ProcessId = entity?.ProcessId,
                ProcessCode = entity?.ProcessCode,
                TransitionId = transition?.TransitionId,
                Phase = NextStepRoutePhase.Preview
            });
        }
    }
}
