
using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Assignee;
using saiflow.engine.Client;
using saiflow.engine.Result;
using saiflow.engine.Runtime;
using saiflow.engine.Pattern;
using saiflow.engine.Event;
using saiflow.engine.Pattern;
using saiflow.engine.Core;
using saiflow.engine.Hooks;

using saiflow.engine.Factories;
namespace saiflow.engine.Pattern
{
    /// <summary>
    /// 节点执行器抽象基类
    /// </summary>
    internal abstract class NodeMediator
    {
        #region 属性

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
                    activityInstanceManager = Sail.ManagerFactory.Activities;
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
                    taskManager = Sail.ManagerFactory.Tasks;
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
                    processInstanceManager = Sail.ProcessInstances;
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

        #region 抽象方法
        /// <summary>
        /// 执行当前节点工作项
        /// </summary>
        internal abstract void ExecuteWorkItem(ActivityInstanceEntity activityInstance);
        #endregion

        #region 构造函数
        protected SailClient Sail { get; }

        internal NodeMediator(ActivityForwardContext forwardContext, SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _activityForwardContext = forwardContext;
            _kit = kit;
            LinkContext.FromActivity = forwardContext.CurrentActivity;
        }

        internal NodeMediator(BackwardContext backwardContext, SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _kit = kit;
            _backwardContext = backwardContext;
            LinkContext.FromActivity = backwardContext.BackwardFromActivity;
        }

        internal NodeMediator(SQLBuilder kit, SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            _kit = kit;
        }
        #endregion

        #region 执行方法
        /// <summary>
        /// 获取事件服务
        /// </summary>
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
                    ProcessInstanceId = ActivityForwardContext.ProcessInstance.Id.Value,
                    ActivityId = fromActivityId,
                    ActivityName = fromActivityName
                };
                _eventService = EventServiceFactory.CreateEventService(EventScopeTypeEnum.Activity, this.Kit, eventContext, Sail);
            }
            return _eventService;
        }

        /// <summary>
        /// 供派生类获取内部事件服务
        /// </summary>
        protected IEventService GetInnerEventService()
        {
            return GetEventService() as IEventService;
        }

        /// <summary>
        /// 判断是否为非开始类型节点
        /// </summary>
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
        /// 执行工作项之前
        /// </summary>
        protected void OnBeforeExecuteWorkItem()
        {
            var eventService = GetEventService();
            var actionList = LinkContext.FromActivity.ActionList;
            ActionExecutor.ExecuteActionListBefore(actionList, eventService as IEventService);

            FireActivityHook(EventFireTypeEnum.OnActivityExecuting);
        }

        /// <summary>
        /// 执行工作项之后
        /// </summary>
        protected void OnAfterExecuteWorkItem()
        {
            var eventService = GetEventService();
            var actionList = ((this is NodeMediatorEnd) || (this is NodeMediatorIntermediate)) ? LinkContext.CurrentActivity.ActionList : LinkContext.FromActivity.ActionList;
            ActionExecutor.ExecuteActionListAfter(actionList, eventService as IEventService);

            FireActivityHook(EventFireTypeEnum.OnActivityExecuted);
        }

        /// <summary>活动生命周期走 Gate/Hook 三轨（不再走 EventSubscriptionList）。</summary>
        protected void FireActivityHook(EventFireTypeEnum eventType, ActivityInstanceEntity activityInstance = null)
        {
            var runner = ActivityForwardContext?.ActivityResource?.AppRunner;
            var processInstance = ActivityForwardContext?.ProcessInstance;
            if (runner == null || processInstance?.Id == null)
                return;

            var ai = activityInstance
                ?? ActivityForwardContext.FromActivityInstance
                ?? LinkContext?.FromActivityInstance;

            var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                Sail,
                Kit,
                runner,
                processInstance.Id.Value,
                ai,
                ActivityForwardContext.TaskId,
                eventType,
                FlowOperation.Run);
            FlowCapabilityPipeline.AfterSuccess(Sail, Kit, hookCtx, eventType);
        }

        /// <summary>
        /// 执行服务节点内容
        /// </summary>
        protected void OnExecutingServiceItem()
        {
            var eventService = GetEventService();
            var serviceList = LinkContext.CurrentActivity.ServiceList;
            ServiceExecutor.ExecuteServiceList(serviceList, this.ActivityForwardContext, eventService as IEventService);
        }

        /// <summary>
        /// 执行 AI 服务节点内容
        /// </summary>
        protected void OnExecutingAIServiceItem(ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity currentActivityInstance)
        {
            var eventService = GetEventService();
            var fromActivity = LinkContext.FromActivity;
            var currentActivity = LinkContext.CurrentActivity;
            AiServiceExecutor.ExecuteAIServiceList(Sail, fromActivity, fromActivityInstance, currentActivity, currentActivityInstance, ActivityForwardContext, eventService as IEventService);
        }

        /// <summary>
        /// 执行脚本节点内容
        /// </summary>
        protected void OnExecutingScriptItem()
        {
            var eventService = GetEventService();
            var scriptList = LinkContext.CurrentActivity.ScriptList;
            ScriptExecutor.ExecuteScriptList(scriptList, eventService as IEventService);
        }
        #endregion

        #region 流程运转逻辑
        /// <summary>
        /// 遍历并执行当前节点的后续节点。
        /// 正常流转需解析 Transition；跳转、退回等直接指定目标，不可等同于正常流转解析。
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
                        throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNode.warn"));
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
                throw new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNode.error", ex.Message),
                    ex.InnerException != null? ex.InnerException : ex);
            }
        }

        /// <summary>
        /// 合并上游/运行时条件键值与当前活动实例已写入的变量
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
        /// 递归执行后续节点：
        /// 1) 创建常规任务节点；
        /// 2) 创建会签（多实例）节点。
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
                    if (XPDLHelper.IsGatewayComponentNode(comp.Activity.ActivityType) == true)
                    {
                        ContinueForwardGatewayNode(fromActivity, fromActivityInstance, comp, conditionKeyValuePair, isNotParsedForward, activityResource, ref mediatorResult);
                    }
                    else if (XPDLHelper.IsCrossOverComponentNode(comp.Activity.ActivityType) == true)
                    {
                        ContinueForwardIntermediateNode(fromActivity, fromActivityInstance, comp, conditionKeyValuePair, isNotParsedForward, activityResource, ref mediatorResult);
                    }
                    else
                    {
                        mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                            WfNodeMediatedFeedback.UnknownNodeTypeToWatch));
                        LogManager.RecordLog(Sail, "NodeMediator.UnknownType.Exception",
                            LogEventType.Exception,
                            LogPriority.Normal,
                            null,
                            new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.unknowntype.warn",
                                comp.Activity.ActivityType.ToString()))
                            );
                    }
                }
                else
                {
                    switch (comp.Activity.ActivityType)
                    {
                        case ActivityTypeEnum.TaskNode:
                            ContinueForwardTaskNode(fromActivity, fromActivityInstance, comp, isNotParsedForward, activityResource, ref mediatorResult);
                            break;
                        case ActivityTypeEnum.AIServiceNode:
                            ContinueForwardAIServiceNode(fromActivity, fromActivityInstance, comp, isNotParsedForward, activityResource, ref mediatorResult);
                            break;
                        case ActivityTypeEnum.ServiceNode:
                            ContinueForwardServiceNode(fromActivity, fromActivityInstance, comp, isNotParsedForward, activityResource, ref mediatorResult);
                            break;
                        case ActivityTypeEnum.ScriptNode:
                            ContinueForwardScriptNode(fromActivity, fromActivityInstance, comp, conditionKeyValuePair, isNotParsedForward, activityResource, ref mediatorResult);
                            break;
                        case ActivityTypeEnum.MultiSignNode:
                            ContinueForwardMultiSignNode(fromActivity, fromActivityInstance, comp, isNotParsedForward, activityResource, ref mediatorResult);
                            break;
                        case ActivityTypeEnum.SubProcessNode:
                            ContinueForwardSubProcessNode(fromActivityInstance, comp, activityResource);
                            break;
                        case ActivityTypeEnum.EndNode:
                            ContinueForwardEndNode(fromActivity, fromActivityInstance, comp, activityResource, ref mediatorResult);
                            break;
                        default:
                            ContinueForwardUnknownLeafNode(comp, ref mediatorResult);
                            break;
                    }
                }
            }
        }

        #region ContinueForward 复合节点处理
        /// <summary>
        /// 网关节点：依据来源活动实例状态判断是否可继续
        /// </summary>
        private void ContinueForwardGatewayNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            IDictionary<string, string> conditionKeyValuePair,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
            {
                // 分支/合并网关：先实例化当前节点（自动完成）
                NodeMediatorGateway gatewayNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorGateway(comp.Activity, this.ActivityForwardContext.ProcessModel, Kit);

                ICompleteGatewayAutomaticlly autoGateway = (ICompleteGatewayAutomaticlly)gatewayNodeMediator;
                NodeAutoExecutedResult nodeExecutedResult = autoGateway.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                    comp.Transition.TransitionId,
                    fromActivity,
                    fromActivityInstance,
                    activityResource.AppRunner,
                    Kit);

                if (nodeExecutedResult.Status == NodeAutoExecutedStatus.Successed)
                {
                    // 继续遍历后续子节点
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
                    LogManager.RecordLog(Sail, "ContinueForwardCurrentNodeRecurisivlyExeception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(
                            Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.warn"))
                        );
                }
            }
            else
            {
                // 下一节点尚未创建，需给出提示
                if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                    LogManager.RecordLog(Sail, "NodeMediator.ContinueForwardCurrentNodeRecurisivly.IsWaintingOneOfJoin.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                        );
                }
                else
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                    LogManager.RecordLog(Sail, "NodeMediator.ContinueForwardCurrentNodeRecurisivly.IsWaintingOneOfJoin.OtherUnknownReason.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                        );
                }
            }
        }

        /// <summary>
        /// 中间事件节点：调用外部业务逻辑后继续向下流转
        /// </summary>
        private void ContinueForwardIntermediateNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            IDictionary<string, string> conditionKeyValuePair,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            NodeMediator eventNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorEvent(ActivityForwardContext, comp.Activity, ActivityForwardContext.ProcessModel, Kit);
            eventNodeMediator.LinkContext.FromActivity = fromActivity;
            eventNodeMediator.LinkContext.CurrentActivity = comp.Activity;

            ICreatedAutomaticlly autoCreatedEvent = (ICreatedAutomaticlly)eventNodeMediator;
            var toActivityInstance = autoCreatedEvent.CreatedAutomaticlly(comp.Activity,
                ActivityForwardContext.ProcessInstance,
                activityResource.AppRunner,
                Kit);

            eventNodeMediator.ExecuteWorkItem(toActivityInstance);

            ICompletedAutomaticlly autoCompletedEvent = (ICompletedAutomaticlly)eventNodeMediator;
            NodeAutoExecutedResult nodeExecutedResult = autoCompletedEvent.CompleteAutomaticlly(ActivityForwardContext.ProcessInstance,
                comp.Transition.TransitionId,
                fromActivity,
                fromActivityInstance,
                comp.Activity,
                toActivityInstance,
                activityResource.AppRunner,
                Kit);

            if (nodeExecutedResult.Status == NodeAutoExecutedStatus.Successed)
            {
                // 继续遍历后续子节点
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
                LogManager.RecordLog(Sail, "IntermediateNodeExecuteException",
                    LogEventType.Exception,
                    LogPriority.Normal,
                    null,
                    new WfRuntimeException(nodeExecutedResult.Status.ToString()));
            }
        }
        #endregion

        #region ContinueForward 叶子节点处理
        /// <summary>
        /// 任务节点：依据来源活动实例状态判断是否可创建任务
        /// </summary>
        private void ContinueForwardTaskNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
            {
                // 创建新任务节点
                NodeMediator taskNodeMediator = new NodeMediatorTask(Kit, Sail);
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
                // 下一任务节点尚未创建，需给出提示
                if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                    LogManager.RecordLog(Sail, "NodeMediator.TaskNode.NeedOtherGatewayBranchesToJoin.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                        );
                }
                else
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                    LogManager.RecordLog(Sail, "NodeMediator.TaskNode.NeedOtherGatewayBranchesToJoin.OtherUnkownReaseon.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                        );
                }
            }
        }

        /// <summary>
        /// AI 服务节点：执行后按输出变量重算后续节点并继续流转
        /// </summary>
        private void ContinueForwardAIServiceNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            NodeAutoExecutedResult aiServiceExecutedResult = null;
            NodeMediator aiServiceNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorEvent(ActivityForwardContext, comp.Activity, ActivityForwardContext.ProcessModel, Kit);

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
                var pvm = new ProcessVariableManager(Sail);
                var currentActivityVariableList = pvm.GetVariableListByActivity(kit, ActivityForwardContext.ProcessInstance.Id.Value, currentActivityInstance.Id.Value,
                    outputVariableNameList);

                var newConditionKeyValuePair = MergeActivityConditions(activityResource, currentActivityVariableList);

                var processModelBPMNCore = new ProcessModelBPMNCore();
                var aiServiceNextMatchedResult = processModelBPMNCore.GetNextActivityTreeListCore(Sail, ActivityForwardContext.ProcessModel, comp.Activity.ActivityId, currentActivityInstance.Id, newConditionKeyValuePair, Kit);

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
                LogManager.RecordLog(Sail, "AIServiceNodeExecuteException",
                    LogEventType.Exception,
                    LogPriority.Normal,
                    null,
                    new WfRuntimeException(aiServiceExecutedResult.Status.ToString()));
            }
        }

        /// <summary>
        /// 服务节点：调用外部业务逻辑后继续向下流转
        /// </summary>
        private void ContinueForwardServiceNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            NodeAutoExecutedResult serviceExecutedResult = null;
            NodeMediator serviceNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorEvent(ActivityForwardContext, comp.Activity, ActivityForwardContext.ProcessModel, Kit);

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
                var pvm = new ProcessVariableManager(Sail);
                var currentActivityVariableList = pvm.GetVariableListByActivity(kit, ActivityForwardContext.ProcessInstance.Id.Value, currentActivityInstance.Id.Value,
                    outputVariableNameList);
                var newConditionKeyValuePair = MergeActivityConditions(activityResource, currentActivityVariableList);

                var processModelBPMNCore = new ProcessModelBPMNCore();
                var serviceNextMatchedResult = processModelBPMNCore.GetNextActivityTreeListCore(Sail, ActivityForwardContext.ProcessModel, comp.Activity.ActivityId, currentActivityInstance.Id, newConditionKeyValuePair, Kit);

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

                // 继续遍历后续子节点
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
                LogManager.RecordLog(Sail, "ServiceNodeExecuteException",
                    LogEventType.Exception,
                    LogPriority.Normal,
                    null,
                    new WfRuntimeException(serviceExecutedResult.Status.ToString()));
            }
        }

        /// <summary>
        /// 脚本节点：执行脚本后继续向下流转
        /// </summary>
        private void ContinueForwardScriptNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            IDictionary<string, string> conditionKeyValuePair,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            NodeAutoExecutedResult scriptExecutedResult = null;
            NodeMediator scriptNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorEvent(ActivityForwardContext, comp.Activity, ActivityForwardContext.ProcessModel, Kit);

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
                LogManager.RecordLog(Sail, "ScriptNodeExecuteException",
                    LogEventType.Exception,
                    LogPriority.Normal,
                    null,
                    new WfRuntimeException(scriptExecutedResult.Status.ToString()));
            }
        }

        /// <summary>
        /// 会签节点：依据来源活动实例状态创建多实例
        /// </summary>
        private void ContinueForwardMultiSignNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            Boolean isNotParsedForward,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
            {
                NodeMediator mediatorMICreator = new NodeMediatorMultiSignCreator(Kit, Sail);
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
                // 下一会签节点尚未创建，需给出提示
                if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                    LogManager.RecordLog(Sail, "ContinueForwardCurrentNodeRecurisivlyExeception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                        );
                }
                else
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                    LogManager.RecordLog(Sail, "ContinueForwardCurrentNodeRecurisivlyExeception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                        );
                }
            }
        }

        /// <summary>
        /// 子流程节点：源实例完成后创建子流程任务转移
        /// </summary>
        private void ContinueForwardSubProcessNode(ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            ActivityResource activityResource)
        {
            if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
            {
                var subNodeMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorSubProcess(comp.Activity, this.Kit);
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

        /// <summary>
        /// 结束节点：完结流程
        /// </summary>
        private void ContinueForwardEndNode(Activity fromActivity,
            ActivityInstanceEntity fromActivityInstance,
            NextActivityComponent comp,
            ActivityResource activityResource,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            if (fromActivityInstance.ActivityState == (short)ActivityStateEnum.Completed)
            {
                var endMediator = SailFactoryAccess.Mediator(Sail).CreateNodeMediatorEnd(ActivityForwardContext, comp.Activity, Kit);
                endMediator.LinkContext.CurrentActivity = comp.Activity;

                // 创建结束节点实例与转移数据
                endMediator.CreateActivityTaskTransitionInstance(comp.Activity, ActivityForwardContext.ProcessInstance,
                    fromActivityInstance, comp.Transition.TransitionId, TransitionTypeEnum.Forward,
                    TransitionFlyingTypeEnum.NotFlying,
                    activityResource,
                    Kit);

                // 执行结束节点
                endMediator.ExecuteWorkItem(null);
            }
            else
            {
                // 结束节点尚未创建，需给出提示
                if (IsWaitingOneOfJoin(fromActivity.GatewayDetail) == true)
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.NeedOtherGatewayBranchesToJoin));
                    LogManager.RecordLog(Sail, "NodeMediator.EndNode.NeedOtherGatewayBranchesToJoin.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.waitingothers.warn"))
                        );
                }
                else
                {
                    mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                        WfNodeMediatedFeedback.OtherUnknownReasonToDebug));
                    LogManager.RecordLog(Sail, "NodeMediator.EndNode.NeedOtherGatewayBranchesToJoin.OtherUnkownReaseon.Exception",
                        LogEventType.Exception,
                        LogPriority.Normal,
                        null,
                        new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.otherreason.warn"))
                        );
                }
            }
        }

        /// <summary>
        /// 未知叶子节点类型
        /// </summary>
        private void ContinueForwardUnknownLeafNode(NextActivityComponent comp,
            ref List<WfNodeMediatedResult> mediatorResult)
        {
            mediatorResult.Add(WfNodeMediatedResult.CreateNodeMediatedResultWithException(
                WfNodeMediatedFeedback.UnknownNodeTypeToWatch));
            LogManager.RecordLog(Sail, "NodeMediator.UnkonwNodeType.Exception",
                LogEventType.Exception,
                LogPriority.Normal,
                null,
                new WfRuntimeException(Sail.Messages.GetEngineMessage("nodemediator.ContinueForwardCurrentNodeRecurisivly.unknowntype.warn",
                    comp.Activity.ActivityType.ToString()))
                );
        }
        #endregion

        /// <summary>
        /// 判断是否处于合并网关等待场景（OrJoin / XOrJoin / AndJoin / AndJoinMI）
        /// </summary>
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
        /// 创建活动、任务与转移实例（派生类按节点类型重写）
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
        /// 创建任务（虚方法）：
        /// 1. 自动执行的工作项通常无需重写；
        /// 2. 人工待办需重写以写入待办任务数据。
        /// </summary>
        internal virtual void CreateNewTask(ActivityInstanceEntity toActivityInstance,
            ActivityResource activityResource,
            SQLBuilder kit)
        {
            // R10：自动审批锚点已入队 → 不插人工待办；回写 AI Id 供 Job 精确认领
            if (activityResource.AutoApprovePending)
            {
                BindAutoApproveAnchorActivityInstance(kit, toActivityInstance);
                return;
            }

            if (activityResource.NextActivityPerformers == null)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediator.createnewtask.warn"));
            }

            var performers = activityResource.NextActivityPerformers[toActivityInstance.ActivityId];
            if (performers == null || performers.Count == 0)
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("nodemediator.createnewtask.warn"));
            }

            TaskManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance, performers, activityResource.AppRunner);
        }

        /// <summary>
        /// 创建活动实例对象
        /// </summary>
        protected ActivityInstanceEntity CreateActivityInstanceObject(Activity activity,
            ProcessInstanceEntity processInstance,
            WfAppRunner runner)
        {
            ActivityInstanceEntity entity = ActivityInstanceManager.CreateActivityInstanceObject(processInstance.AppName,
                processInstance.AppInstanceId,
                processInstance.AppInstanceCode,
                processInstance.ProcessId,
                processInstance.Id.Value,
                activity,
                runner);

            return entity;
        }

        /// <summary>
        /// 插入活动实例
        /// </summary>
        protected virtual int InsertActivityInstance(ActivityInstanceEntity activityInstance,
            SQLBuilder kit)
        {
            return ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), activityInstance);
        }

        /// <summary>
        /// 完成活动实例
        /// </summary>
        protected virtual void CompleteActivityInstance(int ActivityInstanceId,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), ActivityInstanceId, runner);
        }

        /// <summary>
        /// 创建会签主节点及下属多实例子节点
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

            // 经 Assignee 管道定范围（补齐原先未走 Generate 的缺口）
            toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource);

            // R10：会签须有子办理人；零 Performer 自动过不适用 SignTogether
            if (activityResource.AutoApprovePending)
            {
                throw new WfAssigneeException(
                    AssigneeReasonCodes.AssigneeAutoApproveNotForMultiSign,
                    "SignTogether multi-sign cannot auto-approve with zero performers. "
                    + "Configure real assignees or NodePolicy empty=3/4. ReasonCode="
                    + AssigneeReasonCodes.AssigneeAutoApproveNotForMultiSign);
            }

            activityResource.NextActivityPerformers ??= new Dictionary<string, PerformerList>();
            if (!activityResource.NextActivityPerformers.TryGetValue(toActivity.ActivityId, out var plist)
                || plist == null
                || plist.Count == 0)
            {
                throw new WfAssigneeException(
                    AssigneeReasonCodes.AssigneeEmpty,
                    "SignTogether requires at least one performer for activity '"
                    + toActivity.ActivityId
                    + "'. ReasonCode="
                    + AssigneeReasonCodes.AssigneeEmpty);
            }

            toActivityInstance.ActivityState = (short)ActivityStateEnum.Suspended;
            toActivityInstance.ComplexType = (short)ComplexTypeEnum.SignTogether;

            toActivityInstance.MergeType = (short)toActivity.MultiSignDetail.MergeType;
            toActivityInstance.CompareType = (short)toActivity.MultiSignDetail.CompareType;
            toActivityInstance.CompleteOrder = toActivity.MultiSignDetail.CompleteOrder;

            toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(plist);
            toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(plist);

            ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), toActivityInstance);
            InsertTransitionInstance(processInstance,
                transitionId,
                fromActivityInstance,
                toActivityInstance,
                transitionType,
                flyingType,
                activityResource.AppRunner,
                kit);

            // 插入会签子节点实例
            ActivityInstanceEntity entity = new ActivityInstanceEntity();
            for (short i = 0; i < plist.Count; i++)
            {
                entity = ActivityInstanceManager.CreateActivityInstanceObject(toActivityInstance);
                entity.AssignedUserIds = plist[i].UserId;
                entity.AssignedUserNames = plist[i].UserName;
                entity.GroupId = plist[i].GroupId;
                entity.GroupName = plist[i].GroupName;
                entity.GroupRole = plist[i].GroupRole;
                entity.UnitId = plist[i].UnitId;
                entity.UnitCode = plist[i].UnitCode;
                entity.UnitName = plist[i].UnitName;
                entity.MainActivityInstanceId = toActivityInstance.Id;

                // 设置多实例子节点执行顺序（串行 / 并行）
                if (toActivityInstance.MergeType == (short)MergeTypeEnum.Sequence)
                {
                    entity.CompleteOrder = (short)(i + 1);
                }
                else if (toActivityInstance.MergeType == (short)MergeTypeEnum.Parallel)
                {
                    // 并行模式下完成顺序相同，统一为 -1
                    entity.CompleteOrder = -1;       
                }

                // 串行会签：仅首个子节点运行，其余挂起
                if ((i > 0) && (toActivity.MultiSignDetail.MergeType == MergeTypeEnum.Sequence))
                {
                    entity.ActivityState = (short)ActivityStateEnum.Suspended;
                }

                entity.Id = ActivityInstanceManager.Insert(SailSqlAccess.Sibling(kit), entity);
                TaskManager.Insert(SailSqlAccess.Sibling(kit), entity, plist[i], activityResource.AppRunner);
            }
        }

        /// <summary>
        /// 创建退回目标活动实例对象
        /// </summary>
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
                processInstance.Id.Value,
                processInstance.ProcessId,
                this.BackwardContext.BackwardToTaskActivity,
                backwardType,
                backSrcActivityInstanceId,
                backOrgActivityInstanceId,
                runner);

            return entity;
        }

        /// <summary>
        /// 插入转移实例
        /// </summary>
        internal virtual int InsertTransitionInstance(ProcessInstanceEntity processInstance,
            String transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            WfAppRunner runner,
            SQLBuilder kit)
        {
            var tim = new TransitionInstanceManager(Sail);
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

        /// <summary>
        /// 解析并写入目标活动的办理人信息（经 Assignee 管道）
        /// </summary>
        protected ActivityInstanceEntity GenerateActivityAssignedUserInfo(ActivityInstanceEntity toActivityInstance,
            ActivityResource activityResource)
        {
            // R2/R4：已挂管道时只走 Assignee，禁止回落 Definition/Specific/Single
            var pipeline = Sail.AssigneePipeline;
            var activityDef = ActivityForwardContext?.CurrentActivity;
            var hasExplicitResolve = !string.IsNullOrWhiteSpace(activityDef?.AssigneeResolveType);

            if (pipeline != null)
            {
                var invoke = new AssigneeInvokeRequest
                {
                    ProcessId = toActivityInstance.ProcessId
                        ?? activityResource.AppRunner?.ProcessId,
                    Version = activityResource.AppRunner?.Version,
                    ProcessInstanceId = toActivityInstance.ProcessInstanceId,
                    ActivityId = toActivityInstance.ActivityId,
                    ActivityCode = toActivityInstance.ActivityCode,
                    Runner = activityResource.AppRunner,
                    NextActivityPerformers = activityResource.NextActivityPerformers,
                    Kit = Kit
                };

                var handled = pipeline.TryResolveForTaskCreate(invoke, out var resolved);

                if (handled && resolved != null && resolved.Count > 0)
                {
                    activityResource.NextActivityPerformers ??= new Dictionary<string, PerformerList>();
                    activityResource.NextActivityPerformers[toActivityInstance.ActivityId] = resolved;
                    toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(resolved);
                    toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(resolved);
                    activityResource.AutoApprovePending = false;
                    return toActivityInstance;
                }

                // R10：仅锚点、零 Performer — 建活动不建待办，Job 走 RunProcessAuto
                if (handled && invoke.ResultAutoApprovePending)
                {
                    activityResource.NextActivityPerformers ??= new Dictionary<string, PerformerList>();
                    activityResource.NextActivityPerformers[toActivityInstance.ActivityId] =
                        resolved ?? new PerformerList();
                    toActivityInstance.AssignedUserIds = null;
                    toActivityInstance.AssignedUserNames = null;
                    activityResource.AutoApprovePending = true;
                    return toActivityInstance;
                }

                var reason = hasExplicitResolve
                    ? AssigneeReasonCodes.AssigneeEmpty
                    : AssigneeReasonCodes.AssigneeConfigMissing;
                var typeHint = hasExplicitResolve
                    ? activityDef.AssigneeResolveType
                    : "(missing)";
                throw new WfAssigneeException(
                    reason,
                    "Assignee resolve failed for activity '"
                    + toActivityInstance.ActivityId
                    + "' Type='"
                    + typeHint
                    + "'. Configure Resolve ConfigPair in designer (R6) or supply performers. ReasonCode="
                    + reason);
            }

            // R2：未挂管道不再回落 Specific/Definition/Single；须 UseAssignees / UseBizAssignees
            _ = Sail.GetAssigneePipelineRequired();
            return toActivityInstance;

            // 旧双轨（已废弃，保留注释备查）：
            // {
            //     toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(
            //         activityResource.NextActivityPerformers[toActivityInstance.ActivityId]);
            //     toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(
            //         activityResource.NextActivityPerformers[toActivityInstance.ActivityId]);
            // }
            // else if (activityResource.AppRunner.NextPerformerType == NextPerformerIntTypeEnum.Definition)
            // {
            //     var performers = ActivityForwardContext.ProcessModel.GetActivityPerformers(toActivityInstance.ActivityId);
            //     activityResource.NextActivityPerformers = performers;
            //     toActivityInstance.AssignedUserIds = PerformerBuilder.GenerateActivityAssignedUserIDs(
            //         performers[toActivityInstance.ActivityId]);
            //     toActivityInstance.AssignedUserNames = PerformerBuilder.GenerateActivityAssignedUserNames(
            //         performers[toActivityInstance.ActivityId]);
            // }
            // else if (activityResource.AppRunner.NextPerformerType == NextPerformerIntTypeEnum.Single)
            // {
            //     activityResource.NextActivityPerformers = ActivityResource.CreateNextActivityPerformers(
            //         toActivityInstance.ActivityId,
            //         activityResource.AppRunner.UserId,
            //         activityResource.AppRunner.UserName);
            //     toActivityInstance.AssignedUserIds = activityResource.AppRunner.UserId;
            //     toActivityInstance.AssignedUserNames = activityResource.AppRunner.UserName;
            // }
            // else
            // {
            //     throw new ApplicationException(
            //         Sail.Messages.GetEngineMessage("nodemediator.generateactivityassigneduserinfo.warn"));
            // }
            // return toActivityInstance;
        }

        /// <summary>
        /// 根据活动实例已分配人员信息反构 PerformerList
        /// </summary>
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

        /// <summary>
        /// R10：零 Performer 锚点入队时尚无 AI Id；活动插入后回写 Outbox 列供 Job 精确认领。
        /// </summary>
        protected void BindAutoApproveAnchorActivityInstance(SQLBuilder kit, ActivityInstanceEntity activityInstance)
        {
            if (activityInstance?.Id is not > 0
                || activityInstance.ProcessInstanceId is not > 0
                || string.IsNullOrWhiteSpace(activityInstance.ActivityId))
                return;

            try
            {
                new HookOutboxManager(Sail).BindAssigneeAutoApproveActivityInstance(
                    SailSqlAccess.Sibling(kit),
                    activityInstance.ProcessInstanceId.Value,
                    activityInstance.ActivityId,
                    activityInstance.Id.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[Assignee] BindAutoApproveAnchorActivityInstance failed AI={0}: {1}",
                    activityInstance.Id, ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// 按节点执行反馈类型生成提示消息
        /// </summary>
        internal string GetNodeMediatedMessage()
        {
            var message = string.Empty;
            if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.ForwardToNextSequenceTask)
            {
                message = Sail.Messages.GetEngineMessage("nodemediator.getnodemediatedmessage.next.warn");
            }
            else if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.WaitingForCompletedMore)
            {
                message = Sail.Messages.GetEngineMessage("nodemediator.GetNodeMediatedMessage.waiting.warn");
            }
            else if (WfNodeMediatedResult.Feedback == WfNodeMediatedFeedback.NotEnoughApprovalBranchesCount)
            {
                message = Sail.Messages.GetEngineMessage("nodemediator.GetNodeMediatedMessage.notenoughapproval.warn");
            }
            return message;
        }
    }
}
