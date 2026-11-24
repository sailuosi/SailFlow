using System;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.run;
using sailflow.events;
using sailflow.xml;

namespace sailflow.engine.Factories;

/// <summary>
/// A · 默认 Mediator 工厂：Create Mediator；并入原 NodeSendBack / EventService。
/// </summary>
public class DefaultMediatorFactory : IMediatorFactory
{
    private readonly SailClient _sail;
    private readonly MediatorInstanceCreator _mediators;

    public DefaultMediatorFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
        _mediators = new MediatorInstanceCreator(_sail);
    }

    public virtual EventServiceBase CreateEventService(
        EventScopeTypeEnum scopeType, SQLBuilder kit, EventContext context)
    {
        if (scopeType == EventScopeTypeEnum.Process)
            return new ProcessEventService(kit, context, _sail);
        if (scopeType == EventScopeTypeEnum.Activity)
            return new ActivityEventService(kit, context, _sail);
        throw new ApplicationException(
            _sail.Messages.GetEngineMessage("eventservicefactory.CreateEventService.error"));
    }

    public virtual INodeSendBack CreateNodeSendBack(
        SendBackOperation sendbackOperation, SQLBuilder kit)
    {
        NodeSendBack nodeSendBack = null;
        if (sendbackOperation.CurrentNodeOperationType == SendBackOperationTypeEnum.Normal)
        {
            if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.Normal)
            {
                nodeSendBack = new NodeSendBackTask(sendbackOperation, kit, _sail);
            }
            else if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.MISPreviousIsLastOne)
            {
                nodeSendBack = new NodeSendBackToMISPrevious(sendbackOperation, kit, _sail);
            }
            else if (sendbackOperation.PreviousNodeOperationType == SendBackOperationTypeEnum.MIPAllIsInCompletedState)
            {
                nodeSendBack = new NodeSendBackToMIPPrevious(sendbackOperation, kit, _sail);
            }
            else
            {
                throw new WorkflowException(_sail.Messages.GetEngineMessage(
                    "nodesendbackfactory.CreateNodeReverter.error",
                    sendbackOperation.PreviousNodeOperationType.ToString()));
            }
        }
        else if (sendbackOperation.CurrentNodeOperationType == SendBackOperationTypeEnum.MultipleInstance)
        {
            if (sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MISFirstOneIsRunning)
            {
                nodeSendBack = new NodeSendBackMISReady(sendbackOperation, kit, _sail);
            }
            else if (sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MISOneIsRunning)
            {
                nodeSendBack = new NodeSendBackMIS(sendbackOperation, kit, _sail);
            }
            else if (sendbackOperation.CurrentMultipleInstanceDetailType == SendBackOperationTypeEnum.MIPOneIsRunning)
            {
                nodeSendBack = new NodeSendBackMIP(sendbackOperation, kit, _sail);
            }
            else
            {
                throw new WorkflowException(_sail.Messages.GetEngineMessage(
                    "nodesendbackfactory.CreateNodeReverter.error",
                    sendbackOperation.CurrentMultipleInstanceDetailType.ToString()));
            }
        }
        return nodeSendBack;
    }

    public virtual INodeMediator CreateNodeMediator(
        ActivityForwardContext forwardContext, SQLBuilder kit)
        => _mediators.CreateNodeMediator(forwardContext, kit);

    public virtual INodeMediatorGateway CreateNodeMediatorGateway(
        Activity gActivity, IProcessModel processModel, SQLBuilder kit)
        => _mediators.CreateNodeMediatorGateway(gActivity, processModel, kit);

    public virtual INodeMediator CreateNodeMediatorEvent(
        ActivityForwardContext forwardContext,
        Activity eventActivity,
        IProcessModel processModel,
        SQLBuilder kit)
        => _mediators.CreateNodeMediatorEvent(forwardContext, eventActivity, processModel, kit);

    public virtual INodeMediator CreateNodeMediatorEnd(
        ActivityForwardContext forwardContext,
        Activity endActivity,
        SQLBuilder kit)
        => _mediators.CreateNodeMediatorEnd(forwardContext, endActivity, kit);

    public virtual INodeMediator CreateNodeMediatorSubProcess(
        ActivityForwardContext forwardContext, Activity activity, SQLBuilder kit)
        => _mediators.CreateNodeMediatorSubProcess(forwardContext, activity, kit);

    public virtual INodeMediator CreateNodeMediatorSubProcess(Activity activity, SQLBuilder kit)
        => _mediators.CreateNodeMediatorSubProcess(activity, kit);
}
