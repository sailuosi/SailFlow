
using System;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Xpdl;
using saiflow.engine.Manager;
using saiflow.engine.Client;
using saiflow.engine.Core;

namespace saiflow.engine.Pattern
{
    /// <summary>
    /// Mediator 实例创建（A 协作者，挂 <see cref="Factories.DefaultMediatorFactory"/>；非 static 表面）。
    /// </summary>
    internal sealed class MediatorInstanceCreator
    {
        private readonly SailClient _sail;

        public MediatorInstanceCreator(SailClient sail)
        {
            _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        }

        private static bool IsRuleTaskActivity(Activity activity)
        {
            return activity?.RuleConfigDetail != null
                && !string.IsNullOrWhiteSpace(activity.RuleConfigDetail.RuleSetCode);
        }

        /// <summary>
        /// 创建方法
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal NodeMediator CreateNodeMediator(ActivityForwardContext forwardContext, SQLBuilder kit)
        {
            if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.StartNode)        
            {
                if (forwardContext.CurrentActivity.TriggerDetail == null || 
                    forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
                {
                    return new NodeMediatorStart(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorStartTimer(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                {
                    return new NodeMediatorStartConditional(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorStartMsgCatch(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorStartMsgThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(), 
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorStartSignalCatch(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorStartSignalThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.EndNode)
            {
                if (forwardContext.CurrentActivity.TriggerDetail == null || 
                    forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
                {
                    return new NodeMediatorEnd(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorEndTimer(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorEndMsgCatch(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorEndMsgThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorEndSignalCatch(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorEndSignalThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.IntermediateNode)
            {
                if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorInterTimerGo(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                {
                    return new NodeMediatorInterConditionalGo(forwardContext, kit, _sail);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorInterMsgCatchContinue(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorInterMsgThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorInterSignalCatchContinue(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorInterSignalThrow(forwardContext, kit, _sail);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(_sail, forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.TaskNode)        
            {
                //普通任务节点，运行时临时加签变为多实例节点
                //普通任务节点
                var controlParamSheet = forwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                if (controlParamSheet != null)
                {
                    if (!string.IsNullOrEmpty(controlParamSheet.SignForwardType)
                        && controlParamSheet.SignForwardType.ToUpper() != "NONE")
                    {
                        return new NodeMediatorSignForward(forwardContext, kit, _sail);
                    }
                    else
                    {
                        return new NodeMediatorTask(forwardContext, kit, _sail);
                    }
                }
                else if(forwardContext.FromActivityInstance.MainActivityInstanceId != null)
                {
                    //加签子实例
                    return new NodeMediatorMultiSignForward(forwardContext, kit, _sail);
                }
                else
                {
                    return new NodeMediatorTask(forwardContext, kit, _sail);
                } 
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.ServiceNode)      
            {
                if (IsRuleTaskActivity(forwardContext.CurrentActivity))
                    return new NodeMediatorRule(forwardContext, kit, _sail);
                return new NodeMediatorService(forwardContext, kit, _sail);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.AIServiceNode)
            {
                return new NodeMediatorAIService(forwardContext, kit, _sail);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.ScriptNode)
            {
                return new NodeMediatorScript(forwardContext, kit, _sail);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.MultiSignNode)
            {
                if (forwardContext.FromActivityInstance.MainActivityInstanceId != null)
                {
                    if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignTogether)
                    {
                        return new NodeMediatorMultiSignTogether(forwardContext, kit, _sail);
                    }
                    else if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)
                    {
                        return new NodeMediatorMultiSignForward(forwardContext, kit, _sail);
                    }
                    else
                    {
                        throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                    }
                }
                else if (forwardContext.FromActivityInstance.MainActivityInstanceId == null)
                {
                    //加签主节点分发操作
                    if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)
                    {
                        var aim = _sail.ManagerFactory.Activities;
                        var miChildList = aim.GetActivityMulitipleInstanceWithState(forwardContext.FromActivityInstance.Id.Value, forwardContext.FromActivityInstance.ProcessInstanceId.Value);

                        //传入加签动态变量
                        var controlParamSheet = forwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                        if (controlParamSheet != null)
                        {
                            if (!string.IsNullOrEmpty(controlParamSheet.SignForwardType)
                                && controlParamSheet.SignForwardType.ToUpper() != "NONE")
                            {
                                return new NodeMediatorSignForward(forwardContext, kit, _sail);
                            }
                            else
                            {
                                return new NodeMediatorTask(forwardContext, kit, _sail);
                            }
                        }
                        else if (miChildList.Count == 0)
                        {
                            return new NodeMediatorTask(forwardContext, kit, _sail);
                        }
                        else
                        {
                            throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.warn"));
                        }
                    }
                    else
                    {
                        throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                    }
                }
                else
                {
                    throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.SubProcessNode)
            {
                var nodeMediatorSubProcess = CreateNodeMediatorSubProcess(forwardContext, forwardContext.CurrentActivity, kit);
                return nodeMediatorSubProcess;
            }
            else
            {
                throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    forwardContext.CurrentActivity.ActivityType.ToString()));
            }
        }

        /// <summary>
        /// 抛出未知 XML 节点类型异常
        /// </summary>
        /// <param name="activityType"></param>
        /// <param name="triggerType"></param>
        /// <param name="throwCatchDirection"></param>
        /// <exception cref="ApplicationException"></exception>
        private static void ThrowUncerternXmlNodeTypeException(SailClient sail, string activityType, string triggerType, string throwCatchDirection = null)
        {
            if (!string.IsNullOrEmpty(throwCatchDirection))
            {
                throw new ApplicationException(sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1},direction:{2}", activityType, triggerType, throwCatchDirection)
                ));
            }
            else
            {
                throw new ApplicationException(sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1}", activityType, triggerType)
                ));
            }
        }

        /// <summary>
        /// 创建Gateway节点处理类实例
        /// </summary>
        /// <param name="gActivity"></param>
        /// <param name="processModel"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal NodeMediatorGateway CreateNodeMediatorGateway(Activity gActivity, IProcessModel processModel, SQLBuilder kit)
        {
            NodeMediatorGateway nodeMediator = null;
            if (gActivity.ActivityType == ActivityTypeEnum.GatewayNode)
            {
                if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndSplit)
                {
                    nodeMediator = new NodeMediatorAndSplit(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndSplitMI)
                {
                    nodeMediator = new NodeMediatorAndSplitMI(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.OrSplit)
                {
                    nodeMediator = new NodeMediatorOrSplit(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.XOrSplit)
                {
                    nodeMediator = new NodeMediatorXOrSplit(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.ApprovalOrSplit)
                {
                    nodeMediator = new NodeMediatorApprovalOrSplit(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndJoin)
                {
                    nodeMediator = new NodeMediatorAndJoin(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndJoinMI)
                {
                    nodeMediator = new NodeMediatorAndJoinMI(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.OrJoin)
                {
                    nodeMediator = new NodeMediatorOrJoin(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.XOrJoin)
                {
                    nodeMediator = new NodeMediatorXOrJoin(gActivity, processModel, kit, _sail);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.EOrJoin)
                {
                    nodeMediator = new NodeMediatorEOrJoin(gActivity, processModel, kit, _sail);
                }
                else
                {
                    throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorGateway.uncertaingateway.warn", 
                        gActivity.GatewayDetail.DirectionType.ToString()));
                }
            }
            else
            {
                throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorGateway.uncertain.warn",
                    gActivity.ActivityType.ToString()));
            }
            return nodeMediator;
        }

        /// <summary>
        /// 创建中间事件节点处理类实例
        /// </summary>
        internal NodeMediator CreateNodeMediatorEvent(ActivityForwardContext forwardContext, Activity eventActivity, IProcessModel processModel, SQLBuilder kit)
        {
            NodeMediator nodeMediator = null;
            if (eventActivity.ActivityType == ActivityTypeEnum.IntermediateNode)
            {
                if (eventActivity.TriggerDetail != null)
                {
                    if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                    {
                        nodeMediator = new NodeMediatorInterTimer(forwardContext, kit, _sail);
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                    {
                        nodeMediator = new NodeMediatorInterConditional(forwardContext, kit, _sail);
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                    {
                        if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            nodeMediator = new NodeMediatorInterMsgCatch(forwardContext, kit, _sail);
                        }
                        else if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                        {
                            nodeMediator = new NodeMediatorInterMsgThrow(forwardContext, kit, _sail);
                        }
                        else
                        {
                            throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                        }
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                    {
                        if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            nodeMediator = new NodeMediatorInterSignalCatch(forwardContext, kit, _sail);
                        }
                        else if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                        {
                            nodeMediator = new NodeMediatorInterSignalThrow(forwardContext, kit, _sail);
                        }
                        else
                        {
                            throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                        }
                    }
                    else
                    {
                        throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                    }
                }
                else
                {
                    nodeMediator = new NodeMediatorIntermediate(forwardContext, kit, _sail);
                }
            }
            else if(eventActivity.ActivityType == ActivityTypeEnum.ServiceNode)
            {
                if (IsRuleTaskActivity(eventActivity))
                    nodeMediator = new NodeMediatorRule(forwardContext, kit, _sail);
                else
                    nodeMediator = new NodeMediatorService(forwardContext, kit, _sail);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else if(eventActivity.ActivityType == ActivityTypeEnum.AIServiceNode)
            {
                nodeMediator = new NodeMediatorAIService(forwardContext, kit, _sail);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else if (eventActivity.ActivityType == ActivityTypeEnum.ScriptNode)
            {
                nodeMediator = new NodeMediatorScript(forwardContext, kit, _sail);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else
            {
                throw new XmlDefinitionException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                    eventActivity.ActivityType.ToString()));
            }
            return nodeMediator;
        }


        /// <summary>
        /// 创建结束事件节点处理类实例
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="endActivity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal NodeMediator CreateNodeMediatorEnd(ActivityForwardContext forwardContext, 
            Activity endActivity, 
            SQLBuilder kit)
        {
            if (endActivity.TriggerDetail == null 
                || endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
            {
                return new NodeMediatorEnd(forwardContext, kit, _sail);
            }
            else if (endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
            {
                return new NodeMediatorEndTimer(forwardContext, kit, _sail);
            }
            else if (endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
            {
                if (endActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                {
                    return new NodeMediatorEndMsgCatch(forwardContext, kit, _sail);
                }
                else if (endActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                {
                    return new NodeMediatorEndMsgThrow(forwardContext, kit, _sail);
                }
                else
                {
                    throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                        string.Format("ActivityType:{0},trigger:{1}", endActivity.ActivityType.ToString(),
                                                            endActivity.TriggerDetail.TriggerType)
                    ));
                }
            }
            else
            {
                throw new ApplicationException(_sail.Messages.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1}", endActivity.ActivityType.ToString(),
                                                        endActivity.TriggerDetail.TriggerType)
                ));
            }
        }

        /// <summary>
        /// 创建子流程节点处理类实例
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="activity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal NodeMediator CreateNodeMediatorSubProcess(ActivityForwardContext forwardContext,
            Activity activity,
            SQLBuilder kit)
        {
            var subProcessNode = (SubProcessNode)activity.Node;
            if (subProcessNode.SubProcessNested != null)
            {
                return new NodeMediatorSubProcessNested(forwardContext, kit, _sail);
            }
            else
            {
                return new NodeMediatorSubProcess(forwardContext, kit, _sail);
            }
        }

        /// <summary>
        /// 创建子流程节点处理类实例
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal NodeMediator CreateNodeMediatorSubProcess(Activity activity,
            SQLBuilder kit)
        {
            var subProcessNode = (SubProcessNode)activity.Node;
            if (subProcessNode.SubProcessNested != null)
            {
                return new NodeMediatorSubProcessNested(kit, _sail);
            }
            else
            {
                return new NodeMediatorSubProcess(kit, _sail);
            }
        }
    }
}
