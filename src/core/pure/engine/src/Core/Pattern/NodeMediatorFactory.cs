
using System;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Core.Pattern.Auto;
using saiflow.engine.Core.Pattern.Event;
using saiflow.engine.Core.Pattern.Event.Timer;
using saiflow.engine.Core.Pattern.Event.Message;
using saiflow.engine.Core.Pattern.Event.Conditional;
using saiflow.engine.Core.Pattern.Event.Signal;
using saiflow.engine.Core.Pattern.Gateway;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl.Node;

namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Node Mediator Factory
    /// 节点执行器的工厂类
    /// </summary>
    internal class NodeMediatorFactory
    {
        private static bool IsRuleTaskActivity(Activity activity)
        {
            return activity?.RuleConfigDetail != null
                && !string.IsNullOrWhiteSpace(activity.RuleConfigDetail.RuleSetCode);
        }

        /// <summary>
        /// Create Method
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeMediator CreateNodeMediator(ActivityForwardContext forwardContext,
            SQLBuilder kit)
        {
            if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.StartNode)        
            {
                if (forwardContext.CurrentActivity.TriggerDetail == null || 
                    forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
                {
                    return new NodeMediatorStart(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorStartTimer(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                {
                    return new NodeMediatorStartConditional(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorStartMsgCatch(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorStartMsgThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(), 
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorStartSignalCatch(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorStartSignalThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.EndNode)
            {
                if (forwardContext.CurrentActivity.TriggerDetail == null || 
                    forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
                {
                    return new NodeMediatorEnd(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorEndTimer(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorEndMsgCatch(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorEndMsgThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorEndSignalCatch(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorEndSignalThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.IntermediateNode)
            {
                if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                {
                    return new NodeMediatorInterTimerGo(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                {
                    return new NodeMediatorInterConditionalGo(forwardContext, kit);
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorInterMsgCatchContinue(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorInterMsgThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else if (forwardContext.CurrentActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                {
                    if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                    {
                        return new NodeMediatorInterSignalCatchContinue(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                    {
                        return new NodeMediatorInterSignalThrow(forwardContext, kit);
                    }
                    else
                    {
                        ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString(),
                            forwardContext.CurrentActivity.TriggerDetail.MessageDirection.ToString());
                        return null;
                    }
                }
                else
                {
                    ThrowUncerternXmlNodeTypeException(forwardContext.CurrentActivity.ActivityType.ToString(),
                        forwardContext.CurrentActivity.TriggerDetail.TriggerType.ToString());
                    return null;
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.TaskNode)        
            {
                //普通任务节点，运行时临时加签变为多实例节点
                //Normal task node, temporarily signed at runtime to become a multi instance node
                var controlParamSheet = forwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                if (controlParamSheet != null)
                {
                    if (!string.IsNullOrEmpty(controlParamSheet.SignForwardType)
                        && controlParamSheet.SignForwardType.ToUpper() != "NONE")
                    {
                        return new NodeMediatorSignForward(forwardContext, kit);
                    }
                    else
                    {
                        return new NodeMediatorTask(forwardContext, kit);
                    }
                }
                else if(forwardContext.FromActivityInstance.MainActivityInstanceId != null)
                {
                    //加签子实例
                    //Signature forward sub instance
                    return new NodeMediatorMultiSignForward(forwardContext, kit);
                }
                else
                {
                    return new NodeMediatorTask(forwardContext, kit);
                } 
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.ServiceNode)      
            {
                if (IsRuleTaskActivity(forwardContext.CurrentActivity))
                    return new NodeMediatorRule(forwardContext, kit);
                return new NodeMediatorService(forwardContext, kit);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.AIServiceNode)
            {
                return new NodeMediatorAIService(forwardContext, kit);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.ScriptNode)
            {
                return new NodeMediatorScript(forwardContext, kit);
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.MultiSignNode)
            {
                if (forwardContext.FromActivityInstance.MainActivityInstanceId != null)
                {
                    if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignTogether)
                    {
                        return new NodeMediatorMultiSignTogether(forwardContext, kit);
                    }
                    else if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)
                    {
                        return new NodeMediatorMultiSignForward(forwardContext, kit);
                    }
                    else
                    {
                        throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                    }
                }
                else if (forwardContext.FromActivityInstance.MainActivityInstanceId == null)
                {
                    //加签主节点的分发操作
                    //Distribution operation of signing the main node
                    if (forwardContext.CurrentActivity.MultiSignDetail.ComplexType == ComplexTypeEnum.SignForward)
                    {
                        var aim = new ActivityInstanceManager();
                        var miChildList = aim.GetActivityMulitipleInstanceWithState(forwardContext.FromActivityInstance.Id, forwardContext.FromActivityInstance.ProcessInstanceId);

                        //加签的动态变量传入
                        //Pass in the signed dynamic variable
                        var controlParamSheet = forwardContext.ActivityResource.AppRunner.ControlParameterSheet;
                        if (controlParamSheet != null)
                        {
                            if (!string.IsNullOrEmpty(controlParamSheet.SignForwardType)
                                && controlParamSheet.SignForwardType.ToUpper() != "NONE")
                            {
                                return new NodeMediatorSignForward(forwardContext, kit);
                            }
                            else
                            {
                                return new NodeMediatorTask(forwardContext, kit);
                            }
                        }
                        else if (miChildList.Count == 0)
                        {
                            return new NodeMediatorTask(forwardContext, kit);
                        }
                        else
                        {
                            throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.warn"));
                        }
                    }
                    else
                    {
                        throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                    }
                }
                else
                {
                    throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.error"));
                }
            }
            else if (forwardContext.CurrentActivity.ActivityType == ActivityTypeEnum.SubProcessNode)
            {
                var nodeMediatorSubProcess = NodeMediatorFactory.CreateNodeMediatorSubProcess(forwardContext, forwardContext.CurrentActivity, kit);
                return nodeMediatorSubProcess;
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    forwardContext.CurrentActivity.ActivityType.ToString()));
            }
        }

        /// <summary>
        /// Throw uncertern xml node type exception
        /// </summary>
        /// <param name="activityType"></param>
        /// <param name="triggerType"></param>
        /// <param name="throwCatchDirection"></param>
        /// <exception cref="ApplicationException"></exception>
        private static void ThrowUncerternXmlNodeTypeException(string activityType, string triggerType, string throwCatchDirection = null)
        {
            if (!string.IsNullOrEmpty(throwCatchDirection))
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1},direction:{2}", activityType, triggerType, throwCatchDirection)
                ));
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1}", activityType, triggerType)
                ));
            }
        }

        /// <summary>
        /// Create Node Mediator Gateway
        /// 创建Gateway节点处理类实例
        /// </summary>
        /// <param name="gActivity"></param>
        /// <param name="processModel"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeMediatorGateway CreateNodeMediatorGateway(Activity gActivity,
            IProcessModel processModel,
            SQLBuilder kit)
        {
            NodeMediatorGateway nodeMediator = null;
            if (gActivity.ActivityType == ActivityTypeEnum.GatewayNode)
            {
                if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndSplit)
                {
                    nodeMediator = new NodeMediatorAndSplit(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndSplitMI)
                {
                    nodeMediator = new NodeMediatorAndSplitMI(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.OrSplit)
                {
                    nodeMediator = new NodeMediatorOrSplit(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.XOrSplit)
                {
                    nodeMediator = new NodeMediatorXOrSplit(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.ApprovalOrSplit)
                {
                    nodeMediator = new NodeMediatorApprovalOrSplit(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndJoin)
                {
                    nodeMediator = new NodeMediatorAndJoin(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.AndJoinMI)
                {
                    nodeMediator = new NodeMediatorAndJoinMI(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.OrJoin)
                {
                    nodeMediator = new NodeMediatorOrJoin(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.XOrJoin)
                {
                    nodeMediator = new NodeMediatorXOrJoin(gActivity, processModel, kit);
                }
                else if (gActivity.GatewayDetail.DirectionType == GatewayDirectionEnum.EOrJoin)
                {
                    nodeMediator = new NodeMediatorEOrJoin(gActivity, processModel, kit);
                }
                else
                {
                    throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorGateway.uncertaingateway.warn", 
                        gActivity.GatewayDetail.DirectionType.ToString()));
                }
            }
            else
            {
                throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorGateway.uncertain.warn",
                    gActivity.ActivityType.ToString()));
            }
            return nodeMediator;
        }

        /// <summary>
        /// Create Node Mediator Event
        /// 创建中间事件节点处理类实例
        /// </summary>
        internal static NodeMediator CreateNodeMediatorEvent(ActivityForwardContext forwardContext,
            Activity eventActivity,
            IProcessModel processModel,
            SQLBuilder kit)
        {
            NodeMediator nodeMediator = null;
            if (eventActivity.ActivityType == ActivityTypeEnum.IntermediateNode)
            {
                if (eventActivity.TriggerDetail != null)
                {
                    if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                    {
                        nodeMediator = new NodeMediatorInterTimer(forwardContext, kit);
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Conditional)
                    {
                        nodeMediator = new NodeMediatorInterConditional(forwardContext, kit);
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                    {
                        if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            nodeMediator = new NodeMediatorInterMsgCatch(forwardContext, kit);
                        }
                        else if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                        {
                            nodeMediator = new NodeMediatorInterMsgThrow(forwardContext, kit);
                        }
                        else
                        {
                            throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                        }
                    }
                    else if (eventActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Signal)
                    {
                        if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            nodeMediator = new NodeMediatorInterSignalCatch(forwardContext, kit);
                        }
                        else if (eventActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                        {
                            nodeMediator = new NodeMediatorInterSignalThrow(forwardContext, kit);
                        }
                        else
                        {
                            throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                        }
                    }
                    else
                    {
                        throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                                eventActivity.TriggerDetail.MessageDirection.ToString()));
                    }
                }
                else
                {
                    nodeMediator = new NodeMediatorIntermediate(forwardContext, kit);
                }
            }
            else if(eventActivity.ActivityType == ActivityTypeEnum.ServiceNode)
            {
                if (IsRuleTaskActivity(eventActivity))
                    nodeMediator = new NodeMediatorRule(forwardContext, kit);
                else
                    nodeMediator = new NodeMediatorService(forwardContext, kit);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else if(eventActivity.ActivityType == ActivityTypeEnum.AIServiceNode)
            {
                nodeMediator = new NodeMediatorAIService(forwardContext, kit);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else if (eventActivity.ActivityType == ActivityTypeEnum.ScriptNode)
            {
                nodeMediator = new NodeMediatorScript(forwardContext, kit);
                nodeMediator.LinkContext.CurrentActivity = eventActivity;
            }
            else
            {
                throw new XmlDefinitionException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediatorEvent.uncertain.warn",
                    eventActivity.ActivityType.ToString()));
            }
            return nodeMediator;
        }


        /// <summary>
        /// Create Node Mediator End
        /// 创建结束事件节点处理类实例
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="endActivity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeMediator CreateNodeMediatorEnd(ActivityForwardContext forwardContext, 
            Activity endActivity, 
            SQLBuilder kit)
        {
            if (endActivity.TriggerDetail == null 
                || endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.None)
            {
                return new NodeMediatorEnd(forwardContext, kit);
            }
            else if (endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
            {
                return new NodeMediatorEndTimer(forwardContext, kit);
            }
            else if (endActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
            {
                if (endActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                {
                    return new NodeMediatorEndMsgCatch(forwardContext, kit);
                }
                else if (endActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Throw)
                {
                    return new NodeMediatorEndMsgThrow(forwardContext, kit);
                }
                else
                {
                    throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                        string.Format("ActivityType:{0},trigger:{1}", endActivity.ActivityType.ToString(),
                                                            endActivity.TriggerDetail.TriggerType)
                    ));
                }
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediatorfactory.CreateNodeMediator.uncertain.warn",
                    string.Format("ActivityType:{0},trigger:{1}", endActivity.ActivityType.ToString(),
                                                        endActivity.TriggerDetail.TriggerType)
                ));
            }
        }

        /// <summary>
        /// Create Node Mediator Sub Process
        /// 创建子流程节点处理类实例
        /// </summary>
        /// <param name="forwardContext"></param>
        /// <param name="activity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeMediator CreateNodeMediatorSubProcess(ActivityForwardContext forwardContext,
            Activity activity,
            SQLBuilder kit)
        {
            var subProcessNode = (SubProcessNode)activity.Node;
            if (subProcessNode.SubProcessNested != null)
            {
                return new NodeMediatorSubProcessNested(forwardContext, kit);
            }
            else
            {
                return new NodeMediatorSubProcess(forwardContext, kit);
            }
        }

        /// <summary>
        /// Create Node Mediator Sub Process
        /// 创建子流程节点处理类实例
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="kit"></param>
        /// <returns></returns>
        internal static NodeMediator CreateNodeMediatorSubProcess(Activity activity,
            SQLBuilder kit)
        {
            var subProcessNode = (SubProcessNode)activity.Node;
            if (subProcessNode.SubProcessNested != null)
            {
                return new NodeMediatorSubProcessNested(kit);
            }
            else
            {
                return new NodeMediatorSubProcess(kit);
            }
        }
    }
}
