using System;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Core;
using saiflow.engine.Pattern;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 Mediator 工厂：D2 转发现行 static <see cref="NodeMediatorFactory"/>。
/// </summary>
public class DefaultMediatorFactory : IMediatorFactory
{
    private readonly SailClient _sail;

    public DefaultMediatorFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    internal virtual NodeMediator CreateNodeMediator(
        ActivityForwardContext forwardContext, SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediator(forwardContext, kit, _sail);

    internal virtual NodeMediatorGateway CreateNodeMediatorGateway(
        Activity gActivity, IProcessModel processModel, SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediatorGateway(gActivity, processModel, kit, _sail);

    internal virtual NodeMediator CreateNodeMediatorEvent(
        ActivityForwardContext forwardContext,
        Activity eventActivity,
        IProcessModel processModel,
        SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediatorEvent(
            forwardContext, eventActivity, processModel, kit, _sail);

    internal virtual NodeMediator CreateNodeMediatorEnd(
        ActivityForwardContext forwardContext,
        Activity endActivity,
        SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediatorEnd(forwardContext, endActivity, kit, _sail);

    internal virtual NodeMediator CreateNodeMediatorSubProcess(
        ActivityForwardContext forwardContext, Activity activity, SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediatorSubProcess(forwardContext, activity, kit, _sail);

    internal virtual NodeMediator CreateNodeMediatorSubProcess(Activity activity, SQLBuilder kit)
        => NodeMediatorFactory.CreateNodeMediatorSubProcess(activity, kit, _sail);
}
