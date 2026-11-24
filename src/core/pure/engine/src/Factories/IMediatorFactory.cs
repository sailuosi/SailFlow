using mooSQL.data;
using sailflow.run;
using sailflow.events;
using sailflow.xml;

namespace sailflow.factory;
/// <summary>
/// A · Mediator 工厂槽（可覆写）。Create* 返回公开 Mediator / Event / SendBack 表面。
/// </summary>
public interface IMediatorFactory
{
    EventServiceBase CreateEventService(
        EventScopeTypeEnum scopeType, SQLBuilder kit, EventContext context);

    INodeSendBack CreateNodeSendBack(
        SendBackOperation sendbackOperation, SQLBuilder kit);

    INodeMediator CreateNodeMediator(
        ActivityForwardContext forwardContext, SQLBuilder kit);

    INodeMediatorGateway CreateNodeMediatorGateway(
        Activity gActivity, IProcessModel processModel, SQLBuilder kit);

    INodeMediator CreateNodeMediatorEvent(
        ActivityForwardContext forwardContext,
        Activity eventActivity,
        IProcessModel processModel,
        SQLBuilder kit);

    INodeMediator CreateNodeMediatorEnd(
        ActivityForwardContext forwardContext,
        Activity endActivity,
        SQLBuilder kit);

    INodeMediator CreateNodeMediatorSubProcess(
        ActivityForwardContext forwardContext, Activity activity, SQLBuilder kit);

    INodeMediator CreateNodeMediatorSubProcess(Activity activity, SQLBuilder kit);
}
