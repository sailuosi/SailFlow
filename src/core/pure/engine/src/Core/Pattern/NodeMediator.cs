
using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Xpdl.Schedule;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Core.Result;
using saiflow.engine.Core.Runtime;
using saiflow.engine.Core.Pattern.Event;
using saiflow.engine.Core.Pattern.Gateway;
using saiflow.engine.Event;
using saiflow.engine.Core.Pattern.Event.Message;
using saiflow.engine.Core.Pattern.Event.Timer;
using saiflow.engine.Core.Pattern.Event.Conditional;
using saiflow.engine.Core.Pattern.Event.Signal;
using saiflow.engine.Core.Pattern.Auto;

namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Node Mediator Abstract Class
    /// 节点执行器的抽象类
    /// </summary>
    internal abstract class NodeMediator
    {
        #region Property

        private ActivityForwardContext _activityForwardContext;
        protected ActivityForwardContext ActivityForwardContext
        {
            get
            {
                return _activityForwardContext;
            }
        }

        private BackwardContext _backwardContext;
        protected BackwardContext BackwardContext
        {
            get
            {
                return _backwardContext;
            }
        }

        private SQLBuilder _kit;
        protected SQLBuilder Kit
        {
            get
            {
                return _kit;
            }
        }

        private LinkContext _linkContext;
        internal LinkContext LinkContext
        {
            get
            {
                if (_linkContext == null)
                    _linkContext = new LinkContext();

                return _linkContext;
            }
        }

        private ActivityInstanceManager activityInstanceManager;
        internal ActivityInstanceManager ActivityInstanceManager
        {
            get
            {
                if (activityInstanceManager == null)
                {
                    activityInstanceManager = new ActivityInstanceManager();
                }
                return activityInstanceManager;
            }
        }

        private TaskManager taskManager;
        internal TaskManager TaskManager
        {
            get
            {
                if (taskManager == null)
                {
                    taskManager = new TaskManager();
                }
                return taskManager;
            }
        }

        private ProcessInstanceManager processInstanceManager;
        internal ProcessInstanceManager ProcessInstanceManager
        {
            get
            {
                if (processInstanceManager == null)
                    processInstanceManager = new ProcessInstanceManager();
                return processInstanceManager;
            }
        }

        private WfNodeMediatedResult _wfNodeMediatedResult;
        internal WfNodeMediatedResult WfNodeMediatedResult
        {
            get
            {
                if (_wfNodeMediatedResult == null)
                {
                    _wfNodeMediatedResult = new WfNodeMediatedResult();
                }
                return _wfNodeMediatedResult;
            }
            set
            {
                _wfNodeMediatedResult = value;
            }
        }

        private ReturnDataContext _returnDataContext;
        public ReturnDataContext ReturnDataContext
        {
            get
            {
                if (_returnDataContext == null)
                {
                    _returnDataContext = new ReturnDataContext();
                }
                return _returnDataContext;
            }
        }

        private EventServiceBase _eventService;
        #endregion

        #region Abstract Method
        internal abstract void ExecuteWorkItem(ActivityInstanceEntity activityInstance);
        #endregion

        #region Constructor Function
        internal NodeMediator(ActivityForwardContext forwardContext, SQLBuilder kit)
        {
            _activityForwardContext = forwardContext;
            _kit = kit;
            LinkContext.FromActivity = forwardContext.CurrentActivity;
        }

        internal NodeMediator(BackwardContext backwardContext, SQLBuilder kit)
        {
            _kit = kit;
            _backwardContext = backwardContext;
            LinkContext.FromActivity = backwardContext.BackwardFromActivity;
        }

        internal NodeMediator(SQLBuilder kit)
        {
            _kit = kit;
        }
        #endregion

        #region Execute Method
        /// <summary>
        /// Get Event Service
        /// 获取事件服务
        /// </summary>
        /// <returns></returns>
        private EventServiceBase GetEventService()
        {
            if (_eventService == null)
            {
                int? fromActivityInstanceId = null;
                string fromActivityId = string.Empty;
                string fromActivityName = string.Empty;
                if (IsNotNodeMediatorStart(this) == true && ActivityForwardContext.FromActivityInstance != null)
                {
                    fromActivityInstanceId = ActivityForwardContext.FromActivityInstance.Id;
                    fromActivityId = ActivityForwardContext.FromActivityInstance.ActivityId;
                    fromActivityName = ActivityForwardContext.FromActivityInstance.ActivityName;
                }

                var eventContext = new EventContext
                {
                    AppInstanceId = ActivityForwardContext.ProcessInstance.AppInstanceId,
                    ProcessId = ActivityForwardContext.ProcessInstance.ProcessId,
                    ProcessInstanceId = ActivityForwardContext.ProcessInstance.Id,
                    ActivityId = fromActivityId,
                    ActivityName = fromActivityName
                };
                _eventService = EventServiceFactory.CreateEventService(EventScopeTypeEnum.Activity,
                   this.Kit,
                   eventContext);
            }
            return _eventService;
        }

        /// <summary>
        /// Get inner event service for derived mediators.
        /// </summary>
        protected IEventService GetInnerEventService()
        {
            return GetEventService() as IEventService;
        }

        /// <summary>
        /// Determine whether it is a starting type node
        /// 判断是否为开始类型的节点
        /// </summary>
        /// <param name="nodeMediator"></param>
        /// <returns></returns>
        private bool IsNotNodeMediatorStart(NodeMediator nodeMediator)
        {
            if (!(nodeMediator is NodeMediatorStart)
                && !(nodeMediator is NodeMediatorStartMsgCatch)
                && !(nodeMediator is NodeMediatorStartMsgThrow)
                && !(nodeMediator is NodeMediatorStartSignalCatch)
                && !(nodeMediator is NodeMediatorStartSignalThrow)
                && !(nodeMediator is NodeMediatorStartTimer)
                && !(nodeMediator is NodeMediatorStartConditional))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// On before execute work item
        /// 执行工作项前
        /// </summary>
        protected void OnBeforeExecuteWorkItem()
        {
            var eventService = GetEventService();
            var actionList = LinkContext.FromActivity.ActionList;
            ActionExecutor.ExecuteActionListBefore(actionList, eventService as IEventService);

            //Before node flow, invoke the delegation event of activity execution
            //节点流转前，调用活动执行的委托事件
            var currentActivity = LinkContext.CurrentActivity != null ? LinkContext.CurrentActivity : LinkContext.FromActivity;
            EventExecutor.InvokeExternalEvent(this.Kit,
                EventFireTypeEnum.OnActivityExecuting,
                currentActivity,
                ActivityForwardContext);
        }

        protected void OnAfterExecuteWorkItem()
        {
            var eventService = GetEventService();
            var actionList = ((this is NodeMediatorEnd) || (this is NodeMediatorIntermediate)) ? LinkContext.CurrentActivity.ActionList : LinkContext.FromActivity.ActionList;
            ActionExecutor.ExecuteActionListAfter(actionList, eventService as IEventService);

            //After the node flow is completed, invoke the delegation event that the activity completes execution
            //节点流转完成后，调用活动完成执行的委托事件
            var currentActivity = LinkContext.CurrentActivity != null ? LinkContext.CurrentActivity : LinkContext.FromActivity;
            EventExecutor.InvokeExternalEvent(this.Kit,
                EventFireTypeEnum.OnActivityExecuted,
                currentActivity,
                ActivityForwardContext);
        }

        /// <summary>
        /// Execute code to automatically serve content
        /// 执行代码以自动服务内容
        /// </summary>
        protected void OnExecutingServiceItem()
        {
            var eventService = GetEventService();
            var serviceList = LinkContext.CurrentActivity.ServiceList;
            ServiceExecutor.ExecuteServiceList(serviceList, this.ActivityForwardContext, eventService as IEventService);
        }

        protected void OnExecutingAIServiceItem(ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity currentActivityInstance)
        {
            var eventService = GetEventService();
            var fromActivity = LinkContext.FromActivity;
            var currentActivity = LinkContext.CurrentActivity;
            AiServiceExecutor.ExecuteAIServiceList(fromActivity, fromActivityInstance, currentActivity, currentActivityInstance, ActivityForwardContext, eventService as IEventService);
        }

        /// <summary>
        /// Executing script item
        /// 执行脚本项
        /// </summary>
        protected void OnExecutingScriptItem()
        {
            var eventService = GetEventService();
            var scriptList = LinkContext.CurrentActivity.ScriptList;
            ScriptExecutor.ExecuteScriptList(scriptList, eventService as IEventService);
        }
        #endregion

        #region Process Running Logical
        /// <summary>
        /// Traverse and execute the nodes following the current node
        /// Normal operation: Transition parsing flow is required
        /// Other methods are directly specified, such as jump, return, etc., 
        /// which do not require parsing and cannot be equated with normal flow parsing
        /// 遍历并执行当前节点后续的节点
        /// 正常运行：需要转换解析流程
        /// 其他方法直接指定，如跳转、返回等，不需要解析，不能等同于正常流程解析
        /// </summary>
        internal void ContinueForwardCurrentNode(bool isNotParsedByTransition, SQLBuilder kit)
        {
            try
            {
                var mediatorResult = new List<WfNodeMediatedResult>();
                var activityResource = ActivityForwardContext.ActivityResource;

                if (isNotParsedByTransition == true)
                {
                    var root = NextActivityComponentFactory.CreateNextActivityComponent();
                    var nextActivityComponent = NextActivityComponentFactory.CreateNextActivityComponent(
                        this.LinkContext.FromActivity,
                        this.LinkContext.CurrentActivity);

                    root.Add(nextActivityComponent);

                    ContinueForwardCurrentNodeRecurisivly(this.LinkContext.FromActivity,
                        this.LinkContext.FromActivityInstance,
                        root,
                        activityResource.ConditionKeyValuePair,
                        isNotParsedByTransition,
                        activityResource,
                        ref mediatorResult);
                }
                else
                {
                    var nextActivityMatchedResult = this.ActivityForwardContext.ProcessModel.GetNextActivityTreeListRuntime(
                        this.LinkContext.FromActivityInstance.ActivityId,
                        this.LinkContext.FromActivityInstance.Id,
                        activityResource.ConditionKeyValuePair,
                        activityResource,
                        (a, b) => a.NextActivityPerformers.ContainsKey(b.ActivityId),
                        kit);

                    if (nextActivityMatchedResult.MatchedType != NextActivityMatchedType.Successed
                        || nextActivityMatchedResult.Root.HasChildren == false)
                    {
                        throw new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNode.warn"));
                    }
                    
                    ContinueForwardCurrentNodeRecurisivly(this.LinkContext.FromActivity,
                        this.LinkContext.FromActivityInstance,
                        nextActivityMatchedResult.Root,
                        activityResource.ConditionKeyValuePair,
                        isNotParsedByTransition,
                        activityResource,
                        ref mediatorResult);
                }

                if (mediatorResult.Count() > 0)
                {
                    this.WfNodeMediatedResult = mediatorResult[0];
                }
            }
            catch (System.Exception ex)
            {
                throw new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNode.error", ex.Message),
                    ex.InnerException != null? ex.InnerException : ex);
            }
        }

        /// <summary>
        /// Merge upstream/runtime condition pairs with variables written for the current activity instance.
        /// </summary>
        private static IDictionary<string, string> MergeActivityConditions(ActivityResource activityResource, IList<ProcessVariableEntity> currentActivityRows)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (activityResource?.ConditionKeyValuePair != null)
            {
                foreach (var kv in activityResource.ConditionKeyValuePair)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                        merged[kv.Key] = kv.Value ?? string.Empty;
                }
            }
            if (currentActivityRows != null)
            {
                foreach (var item in currentActivityRows)
                {
                    if (!string.IsNullOrEmpty(item.Name))
                        merged[item.Name] = item.Value ?? string.Empty;
                }
            }
            return merged;
        }

        /// <summary>
        /// Recursive execution node
        /// 1) Task of creating regular nodes
        /// 2) Task of creating a countersignature node
        /// 递归执行节点
        /// 1) 创建常规节点的任务
        /// 2) 创建会签节点的任务
        /// </summary>
        protected void ContinueForwardCurrentNodeRecurisivly(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent root,
            IDictionary<string, string> conditionKeyValuePair,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            foreach (NextActivityComponent comp in root)
            {
                if (comp.HasChildren)
                {
                    NodeAutoExecutedResult nodeExecutedResult = null;
                    if (XPDLHelper.IsGatewayComponentNode(comp.Activity.ActivityType) == true)
                    {
                        //This node type is a task node: Determine whether a task can be created based on the type of from Activity Instance
                        if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
                        {
                            //This node type is a branch or merge node type: First, the current node needs to be instantiated (auto complete)
                            NodeMediatorGateway gatewayNodeMediator = NodeMediatorFactory.CreateNodeMediatorGateway(comp.Activity,
                            this.ActivityForwardContext.ProcessModel,
                            Kit);

                            ICompleteGatewayAutomaticlly autoGateway = (ICompleteGatewayAutomaticlly)gatewayNodeMediator;
                            nodeExecutedResult = autoGateway.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                                comp.Transition.TransitionId,
                                fromActivity,
                                fromActivityInstance,
                                activityResource.AppRunner,
                                Kit);

                            if (nodeExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                            {
                                //Traverse subsequent child nodes
                                ContinueForwardCurrentNodeRecurisivly(gatewayNodeMediator.GatewayActivity,
                                    gatewayNodeMediator.GatewayActivityInstance,
                                    comp,
                                    conditionKeyValuePair,
                                    isNotParsedForward,
                                    activityResource,
                                    ref mediatorResult);
                            }
                            else
                            {
                                mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                    WfNodeMediatedFeedback.OrJoinOneBranchHasBeenFinishedWaittingOthers));
                                LogManager.RecordLog("ContinueForwardCurrentNodeRecurisivlyExeception",
                                    LogEventType.Exception,
                                    LogPriority.Normal,
                                    null,
                                    new WfRuntimeException(
                                        LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.warn"))
                                    );
                            }
                        }
                        else
                        {
                            //The next task node has not been created, and a prompt message is required
                            if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                            {
                                mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                    WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                                LogManager.RecordLog("NodeMediator.ContinueForwardCurrentNodeRecurisivly.IsWaintingOneOfJoin.Exception",
                                    LogEventType.Exception,
                                    LogPriority.Normal,
                                    null,
                                    new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                                    );
                            }
                            else
                            {
                                mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                    WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                                LogManager.RecordLog("NodeMediator.ContinueForwardCurrentNodeRecurisivly.IsWaintingOneOfJoin.OtherUnknownReason.Exception",
                                    LogEventType.Exception,
                                    LogPriority.Normal,
                                    null,
                                    new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                                    );
                            }
                        }
                    }
                    else if (XPDLHelper.IsCrossOverComponentNode(comp.Activity.ActivityType) == true)
                    {
                        //Intermediate event type node, calling external business logic,
                        //and then the process continues to flow downwards
                        NodeMediator eventNodeMediator = NodeMediatorFactory.CreateNodeMediatorEvent(ActivityForwardContext,
                            comp.Activity,
                            ActivityForwardContext.ProcessModel,
                            Kit);
                        eventNodeMediator.LinkContext.FromActivity = fromActivity;
                        eventNodeMediator.LinkContext.CurrentActivity = comp.Activity;

                        ICreatedAutomaticlly autoCreatedEvent = (ICreatedAutomaticlly)eventNodeMediator;
                        var toActivityInstance = autoCreatedEvent.CreatedAutomaticlly(comp.Activity, 
                            ActivityForwardContext.ProcessInstance,
                            activityResource.AppRunner,
                            Kit);

                        eventNodeMediator.ExecuteWorkItem(toActivityInstance);

                        ICompletedAutomaticlly autoCompletedEvent = (ICompletedAutomaticlly)eventNodeMediator;
                        nodeExecutedResult = autoCompletedEvent.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                            comp.Transition.TransitionId,
                            fromActivity,
                            fromActivityInstance,
                            comp.Activity,
                            toActivityInstance,
                            activityResource.AppRunner,
                            Kit);

                        if (nodeExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                        {
                            //Traverse subsequent child nodes
                            ContinueForwardCurrentNodeRecurisivly(eventNodeMediator.LinkContext.CurrentActivity,
                                toActivityInstance,
                                comp,
                                conditionKeyValuePair,
                                isNotParsedForward,
                                activityResource,
                                ref mediatorResult);
                        }
                        else
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.IntermediateEventFailed));
                            LogManager.RecordLog("IntermediateNodeExecuteException",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(nodeExecutedResult.Status.ToString()));
                        }
                    }
                    else
                    {
                        mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                            WfNodeMediatedFeedback.UnknownNodeTypeToWatch));
                        LogManager.RecordLog("NodeMediator.UnknownType.Exception",
                            LogEventType.Exception,
                            LogPriority.Normal,
                            null,
                            new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.unknowntype.warn",
                                comp.Activity.ActivityType.ToString()))
                            );
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.TaskNode)                  
                {
                    //This node type is a task node: Determine whether a task can be created based on the type of from Activity Instance
                    if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
                    {
                        //Create new task node
                        NodeMediator taskNodeMediator = new NodeMediatorTask(Kit);
                        taskNodeMediator.CreateActivityTaskTransitionInstance(comp.Activity,
                            ActivityForwardContext.ProcessInstance,
                            fromActivityInstance,
                            comp.Transition.TransitionId,
                            comp.Transition.DirectionType == TransitionDirectionTypeEnum.Loop ?
                                TransitionTypeEnum.Loop : TransitionTypeEnum.Forward, 
                            isNotParsedForward == true ?
                                TransitionFlyingTypeEnum.ForwardFlying : TransitionFlyingTypeEnum.NotFlying,
                            activityResource,
                            Kit);
                    }
                    else
                    {
                        //The next task node has not been created, and a prompt message is required
                        if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                            LogManager.RecordLog("NodeMediator.TaskNode.NeedOtherGatewayBranchesToJoin.Exception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                                );
                        }
                        else
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                            LogManager.RecordLog("NodeMediator.TaskNode.NeedOtherGatewayBranchesToJoin.OtherUnkownReaseon.Exception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                                );
                        }
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.AIServiceNode)
                {
                    NodeAutoExecutedResult aiServiceExecutedResult = null;
                    NodeMediator aiServiceNodeMediator = NodeMediatorFactory.CreateNodeMediatorEvent(ActivityForwardContext,
                        comp.Activity,
                        ActivityForwardContext.ProcessModel,
                        Kit);

                    ICreatedAutomaticlly autoCreatedEvent = (ICreatedAutomaticlly)aiServiceNodeMediator;
                    var currentActivityInstance = autoCreatedEvent.CreatedAutomaticlly(comp.Activity,
                        ActivityForwardContext.ProcessInstance,
                        activityResource.AppRunner,
                        Kit);

                    aiServiceNodeMediator.ExecuteWorkItem(currentActivityInstance);

                    ICompletedAutomaticlly autoEvent = (ICompletedAutomaticlly)aiServiceNodeMediator;
                    aiServiceExecutedResult = autoEvent.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                        comp.Transition.TransitionId,
                        fromActivity,
                        fromActivityInstance,
                        comp.Activity,
                        currentActivityInstance,
                        activityResource.AppRunner,
                        Kit);

                    if (aiServiceExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                    {
                        var outputVariableNameList = ActivityForwardContext.ProcessModel.GetActivityOutputVarialbeNameList(comp.Activity);

                        var kit = SailSqlAccess.Sibling(Kit);
                        var pvm = new ProcessVariableManager();
                        var currentActivityVariableList = pvm.GetVariableListByActivity(kit, ActivityForwardContext.ProcessInstance.Id, currentActivityInstance.Id,
                            outputVariableNameList);

                        var newConditionKeyValuePair = MergeActivityConditions(activityResource, currentActivityVariableList);

                        var processModelBPMNCore = new ProcessModelBPMNCore();
                        var aiServiceNextMatchedResult = processModelBPMNCore.GetNextActivityTreeListCore(ActivityForwardContext.ProcessModel,
                            comp.Activity.ActivityId, currentActivityInstance.Id, newConditionKeyValuePair, Kit);

                        if (aiServiceNextMatchedResult.MatchedType != NextActivityMatchedType.Successed
                            || aiServiceNextMatchedResult.Root == null
                            || aiServiceNextMatchedResult.Root.HasChildren == false)
                        {
                            throw new WfRuntimeException(
                                "RuleTask/ServiceTask/AI output variables did not match any outgoing transition conditions.");
                        }

                        var newActivityResource = ActivityResourceFactory.Create(ActivityForwardContext.ProcessModel,
                            aiServiceNextMatchedResult.Root,
                            activityResource.AppRunner,
                            newConditionKeyValuePair);

                        ContinueForwardCurrentNodeRecurisivly(aiServiceNodeMediator.LinkContext.CurrentActivity,
                            currentActivityInstance,
                            aiServiceNextMatchedResult.Root,
                            newConditionKeyValuePair,
                            isNotParsedForward,
                            newActivityResource,
                            ref mediatorResult);
                    }
                    else
                    {
                        mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                            WfNodeMediatedFeedback.IntermediateEventFailed));
                        LogManager.RecordLog("AIServiceNodeExecuteException",
                            LogEventType.Exception,
                            LogPriority.Normal,
                            null,
                            new WfRuntimeException(aiServiceExecutedResult.Status.ToString()));
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.ServiceNode)
                {
                    //Service type node, calling external business logic, and then the process continues to flow downwards
                    NodeAutoExecutedResult serviceExecutedResult = null;
                    NodeMediator serviceNodeMediator = NodeMediatorFactory.CreateNodeMediatorEvent(ActivityForwardContext,
                        comp.Activity,
                        ActivityForwardContext.ProcessModel,
                        Kit);

                    ICreatedAutomaticlly autoCreatedEvent = (ICreatedAutomaticlly)serviceNodeMediator;
                    var currentActivityInstance = autoCreatedEvent.CreatedAutomaticlly(comp.Activity,
                        ActivityForwardContext.ProcessInstance,
                        activityResource.AppRunner,
                        Kit);

                    serviceNodeMediator.ExecuteWorkItem(currentActivityInstance);

                    ICompletedAutomaticlly autoEvent = (ICompletedAutomaticlly)serviceNodeMediator;
                    serviceExecutedResult = autoEvent.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                        comp.Transition.TransitionId,
                        fromActivity,
                        fromActivityInstance,
                        comp.Activity,
                        currentActivityInstance,
                        activityResource.AppRunner,
                        Kit);

                    if (serviceExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                    {
                        var outputVariableNameList = ActivityForwardContext.ProcessModel.GetActivityOutputVarialbeNameList(comp.Activity);

                        var kit = SailSqlAccess.Sibling(Kit);
                        var pvm = new ProcessVariableManager();
                        var currentActivityVariableList = pvm.GetVariableListByActivity(kit, ActivityForwardContext.ProcessInstance.Id, currentActivityInstance.Id,
                            outputVariableNameList);
                        var newConditionKeyValuePair = MergeActivityConditions(activityResource, currentActivityVariableList);

                        var processModelBPMNCore = new ProcessModelBPMNCore();
                        var serviceNextMatchedResult = processModelBPMNCore.GetNextActivityTreeListCore(ActivityForwardContext.ProcessModel,
                            comp.Activity.ActivityId, currentActivityInstance.Id, newConditionKeyValuePair, Kit);

                        if (serviceNextMatchedResult.MatchedType != NextActivityMatchedType.Successed
                            || serviceNextMatchedResult.Root == null
                            || serviceNextMatchedResult.Root.HasChildren == false)
                        {
                            throw new WfRuntimeException(
                                "RuleTask/ServiceTask output variables did not match any outgoing transition conditions.");
                        }

                        var newActivityResource = ActivityResourceFactory.Create(ActivityForwardContext.ProcessModel,
                            serviceNextMatchedResult.Root,
                            activityResource.AppRunner,
                            newConditionKeyValuePair);

                        //Traverse subsequent child nodes
                        ContinueForwardCurrentNodeRecurisivly(serviceNodeMediator.LinkContext.CurrentActivity,
                            currentActivityInstance,
                            serviceNextMatchedResult.Root,
                            newConditionKeyValuePair,
                            isNotParsedForward,
                            newActivityResource,
                            ref mediatorResult);
                    }
                    else
                    {
                        mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                            WfNodeMediatedFeedback.IntermediateEventFailed));
                        LogManager.RecordLog("ServiceNodeExecuteException",
                            LogEventType.Exception,
                            LogPriority.Normal,
                            null,
                            new WfRuntimeException(serviceExecutedResult.Status.ToString()));
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.ScriptNode)
                {
                    //Script type node, calling script program, and then the process continues to flow downwards
                    NodeAutoExecutedResult scriptExecutedResult = null;
                    NodeMediator scriptNodeMediator = NodeMediatorFactory.CreateNodeMediatorEvent(ActivityForwardContext,
                        comp.Activity,
                        ActivityForwardContext.ProcessModel,
                        Kit);

                    ICreatedAutomaticlly autoCreatedEvent = (ICreatedAutomaticlly)scriptNodeMediator;
                    var toActivityInstance = autoCreatedEvent.CreatedAutomaticlly(comp.Activity,
                        ActivityForwardContext.ProcessInstance,
                        activityResource.AppRunner,
                        Kit);

                    scriptNodeMediator.ExecuteWorkItem(toActivityInstance);

                    ICompletedAutomaticlly autoEvent = (ICompletedAutomaticlly)scriptNodeMediator;
                    scriptExecutedResult = autoEvent.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                        comp.Transition.TransitionId,
                        fromActivity,
                        fromActivityInstance,
                        comp.Activity,
                        toActivityInstance,
                        activityResource.AppRunner,
                        Kit);
                    if (scriptExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                    {
                        ContinueForwardCurrentNodeRecurisivly(scriptNodeMediator.LinkContext.CurrentActivity,
                            toActivityInstance,
                            comp,
                            conditionKeyValuePair,
                            isNotParsedForward,
                            activityResource,
                            ref mediatorResult);
                    }
                    else
                    {
                        mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(WfNodeMediatedFeedback.IntermediateEventFailed));
                        LogManager.RecordLog("ScriptNodeExecuteException",
                            LogEventType.Exception,
                            LogPriority.Normal,
                            null,
                            new WfRuntimeException(scriptExecutedResult.Status.ToString()));
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.MultiSignNode)
                {
                    if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
                    {
                        //This node type is a task node: Determine whether a task can be created based on the type of from Activity Instance
                        NodeMediator mediatorMICreator = new NodeMediatorMultiSignCreator(Kit);
                        mediatorMICreator.CreateActivityTaskTransitionInstance(comp.Activity,
                            ActivityForwardContext.ProcessInstance,
                            fromActivityInstance,
                            comp.Transition.TransitionId,
                            comp.Transition.DirectionType == TransitionDirectionTypeEnum.Loop ?
                                TransitionTypeEnum.Loop : TransitionTypeEnum.Forward,
                            isNotParsedForward == true ?
                                TransitionFlyingTypeEnum.ForwardFlying : TransitionFlyingTypeEnum.NotFlying,
                            activityResource,
                            Kit);
                    }
                    else
                    {
                        ////The next task node has not been created, and a prompt message is required
                        if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                            LogManager.RecordLog("ContinueForwardCurrentNodeRecurisivlyExeception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                                );
                        }
                        else
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                            LogManager.RecordLog("ContinueForwardCurrentNodeRecurisivlyExeception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                                );
                        }
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.SubProcessNode)
                {
                    if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
                    {
                        var subNodeMediator = NodeMediatorFactory.CreateNodeMediatorSubProcess(comp.Activity, this.Kit);
                        subNodeMediator.CreateActivityTaskTransitionInstance(comp.Activity,
                            ActivityForwardContext.ProcessInstance,
                            fromActivityInstance,
                            comp.Transition.TransitionId,
                            comp.Transition.DirectionType == TransitionDirectionTypeEnum.Loop ?
                                TransitionTypeEnum.Loop : TransitionTypeEnum.Forward,
                            TransitionFlyingTypeEnum.NotFlying,
                            activityResource,
                            Kit);
                    }
                }
                else if (comp.Activity.ActivityType == ActivityTypeEnum.EndNode)
                {
                    if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
                    {
                        //This node is the completion end node, ending the process
                        var endMediator = NodeMediatorFactory.CreateNodeMediatorEnd(ActivityForwardContext, comp.Activity, Kit);
                        endMediator.LinkContext.CurrentActivity = comp.Activity;

                        //Create end node instance and transitioni data
                        endMediator.CreateActivityTaskTransitionInstance(comp.Activity, ActivityForwardContext.ProcessInstance,
                            fromActivityInstance, comp.Transition.TransitionId, TransitionTypeEnum.Forward,
                            TransitionFlyingTypeEnum.NotFlying,
                            activityResource,
                            Kit);

                        //Create end node instance and transfer data
                        endMediator.ExecuteWorkItem(null);
                    }
                    else
                    {
                        //End node not created, prompt message is required
                        if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                            LogManager.RecordLog("NodeMediator.EndNode.NeedOtherGatewayBranchesToJoin.Exception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                                );
                        }
                        else
                        {
                            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                                WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                            LogManager.RecordLog("NodeMediator.EndNode.NeedOtherGatewayBranchesToJoin.OtherUnkownReaseon.Exception",
                                LogEventType.Exception,
                                LogPriority.Normal,
                                null,
                                new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                                );
                        }
                    }
                }
                else
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.UnknownNodeTypeToWatch));
                    LogManager.RecordLog("NodeMediator.UnkonwNodeType.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(LocalizeHelper.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.unknowntype.warn",
                            comp.Activity.ActivityType.ToString()))
                        );
                }
            }
        }

        /// <summary>
        /// Determine whether it is a merger situation
        /// </summary>
        /// <param name="gatewayDetail"></param>
        /// <returns></returns>
        private Boolean IsWaitingOneOfJoin(GatewayDetail gatewayDetail)
        {
            Boolean isWaiting = false;
            if (gatewayDetail != null)
            {
                var directionEnum = gatewayDetail.DirectionType;
                if (directionEnum == GatewayDirectionEnum.OrJoin
                    || directionEnum == GatewayDirectionEnum.XOrJoin
                    || directionEnum == GatewayDirectionEnum.AndJoin
                    || directionEnum == GatewayDirectionEnum.AndJoinMI)
                {
                    isWaiting = true;
                }
            }
            return isWaiting;
        }

        /// <summary>
        /// Create activity task transtion instance
        /// </summary>
        internal virtual void CreateActivityTaskTransitionInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            String transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        { }

        /// <summary>
        /// Virtual method for creating tasks
        /// 1.  For automatically executed work items, there is no need to rewrite this method
        /// 2.  For manually executed work items, the method needs to be rewritten to insert pending task data
        /// </summary>
        /// <param name="activityResource"></param>
        /// <param name="toActivityInstance"></param>
        /// <param name="kit"></param>
        internal virtual void CreateNewTask(ActivityInstanceEntity toActivityInstance,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            if (activityResource.NextActivityPerformers == null)
            {
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("nodemediator.createnewtask.warn"));
            }

            TaskManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance, activityResource.NextActivityPerformers[toActivityInstance.ActivityId], activityResource.AppRunner);
        }

        /// <summary>
        /// Create activity instance object
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="activity"></param>
        /// <param name="runner"></param>
        protected ActivityInstanceEntity CreateActivityInstanceObject(Activity activity,
            ProcessInstanceEntity processInstance,
            WfAppRunner runner)
        {
            ActivityInstanceEntity entity = ActivityInstanceManager.CreateActivityInstanceObject(processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.ProcessId,
                processInstance.Id,
                activity,
                runner);

            return entity;
        }

        protected virtual int InsertActivityInstance(ActivityInstanceEntity activityInstance,
            SQLBuilder kit)
        {
            return ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), activityInstance);
        }


        protected virtual void CompleteActivityInstance(int ActivityInstanceId,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), ActivityInstanceId, runner);
        }

        /// <summary>
        /// Master node of countersignature type, handling multiple instance nodes
        /// Create the master node for the countersignature node and record the instance child nodes under the countersignature master node
        /// </summary>
        internal void CreateMultipleInstance(Activity toActivity,
            ProcessInstanceEntity processInstance,
            ActivityInstanceEntity fromActivityInstance,
            String transitionId,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            var toActivityInstance = CreateActivityInstanceObject(toActivity,
                processInstance, activityResource.AppRunner);

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            toActivityInstance.ComplexType = (short)ComplexTypeEnum.SignTogether;

            toActivityInstance.MergeType = (short)toActivity.MultiSignDetail.MergeType;
            toActivityInstance.CompareType = (short)toActivity.MultiSignDetail.CompareType;
            toActivityInstance.CompleteOrder = toActivity.MultiSignDetail.CompleteOrder;

            toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(
                activityResource.NextActivityPerformers[toActivity.ActivityId]);
            toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(
                activityResource.NextActivityPerformers[toActivity.ActivityId]);

            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);
            InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);

            //Insert signature sub node instance data
            var plist = activityResource.NextActivityPerformers[toActivity.ActivityId];
            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                entity = ActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                entity.AssignedUserIds = plist[i].UserId;
                entity.AssignedUserNames = plist[i].UserName;
                entity.MainActivityInstanceId = toActivityInstance.Id;

                //Setting the Execution Order of Multiple Real Example Nodes in Parallel Serial
                if (toActivityInstance.MergeType == (short)MergeTypeEnum.Sequence)
                {
                    entity.CompleteOrder = (short)(i + 1);
                }
                else if (toActivityInstance.MergeType == (short)MergeTypeEnum.Parallel)
                {
                    //In parallel mode, the priority of Completed Order is the same, so it is set to -1
                    entity.CompleteOrder = -1;       
                }

                //If it is a serial countersignature, only the first node is running, and the other nodes are suspended
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.Id = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);
                TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);
            }
        }

        protected ActivityInstanceEntity CreateBackwardToActivityInstanceObject(ProcessInstanceEntity processInstance,
            BackwardTypeEnum backwardType,
            int backSrcActivityInstanceId,
            int backOrgActivityInstanceId,
            WfAppRunner runner)
        {
            ActivityInstanceEntity entity = ActivityInstanceManager.CreateBackwardActivityInstanceObject(
                processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.Id,
                processInstance.ProcessId,
                this.BackwardContext.BackwardToTaskActivity,
                backwardType,
                backSrcActivityInstanceId,
                backOrgActivityInstanceId,
                runner);

            return entity;
        }

        internal virtual int InsertTransitionInstance(ProcessInstanceEntity processInstance,
            String transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            var tim = new TransitionInstanceManager();
            var transitionInstanceObject = tim.CreateTransitionInstanceObject(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                runner,
                (byte)ConditionParseResultEnum.Passed);
            var newId = tim.Insert(SailSqlAccess.Sibling(kit), transitionInstanceObject);

            return newId;
        }

        protected ActivityInstanceEntity GenerateActivityAssignedUserInfo(ActivityInstanceEntity toActivityInstance,
            ActivityResource activityResource)
        {
            if (activityResource.AppRunner.NextPerformerType == NextPerformerIntTypeEnum.Specific
                && activityResource.NextActivityPerformers != null)
            {
                //The front-end explicitly specifies the list of executing users for the next step
                toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(
                    activityResource.NextActivityPerformers[toActivityInstance.ActivityId]);
                toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(
                    activityResource.NextActivityPerformers[toActivityInstance.ActivityId]);
            }
            else if (activityResource.AppRunner.NextPerformerType ==  NextPerformerIntTypeEnum.Definition)
            {
                //Obtain the list of executing users for the next step based on the role definition on the node
                var performers = ActivityForwardContext.ProcessModel.GetActivityPerformers(toActivityInstance.ActivityId);
                activityResource.NextActivityPerformers = performers;

                toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(
                    performers[toActivityInstance.ActivityId]);
                toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(
                    performers[toActivityInstance.ActivityId]);
            }
            else if (activityResource.AppRunner.NextPerformerType == NextPerformerIntTypeEnum.Single)
            {
                //Single user list for testing purposes
                activityResource.NextActivityPerformers = ActivityResource.CreateNextActivityPerformers(toActivityInstance.ActivityId,
                    activityResource.AppRunner.UserId,
                    activityResource.AppRunner.UserName);

                toActivityInstance.AssignedUserIds = activityResource.AppRunner.UserId;
                toActivityInstance.AssignedUserNames = activityResource.AppRunner.UserName;
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("nodemediator.generateactivityassigneduserinfo.warn"));
            }
            return toActivityInstance;
        }

        /// <summary>
        /// Generate PerformerList data structure based on personnel information assigned by nodes
        /// </summary>
        /// <param name="activityInstance"></param>
        /// <returns></returns>
        protected PerformerList AntiGenerateActivityPerformerList(ActivityInstanceEntity activityInstance)
        {
            var performerList = new PerformerList();

            if (!string.IsNullOrEmpty(activityInstance.AssignedUserIds)
                && !string.IsNullOrEmpty(activityInstance.AssignedUserNames))
            {
                var assignedUserIDs = activityInstance.AssignedUserIds.Split(',');
                var assignedUserNames = activityInstance.AssignedUserNames.Split(',');

                for (var i = 0; i < assignedUserIDs.Count(); i++)
                {
                    performerList.Add(new Performer(assignedUserIDs[i], assignedUserNames[i]));
                }
            }
            return performerList;
        }
        #endregion

        /// <summary>
        /// Generate messages based on the type of node execution result
        /// </summary>
        /// <returns></returns>
        internal string GetNodeMediatedMessage()
        {
            var message = string.Empty;
            if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.ForwardToNextSequenceTask)
            {
                message = LocalizeHelper.GetEngineMessage("nodemediator.getnodemediatedmessage.next.warn");
            }
            else if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.WaitingForCompletedMore)
            {
                message = LocalizeHelper.GetEngineMessage("nodemediator.GetNodeMediatedMessage.waiting.warn");
            }
            else if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount)
            {
                message = LocalizeHelper.GetEngineMessage("nodemediator.GetNodeMediatedMessage.notenoughapproval.warn");
            }
            return message;
        }
    }
}
