using System;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Pattern;

/// <summary>
/// B · 节点 Mediator 执行表面（工厂 Create 产物）。
/// </summary>
public interface INodeMediator
{
    LinkContext LinkContext { get; }

    void ExecuteWorkItem(ActivityInstanceEntity activityInstance);

    void CreateActivityTaskTransitionInstance(
        Activity toActivity,
        ProcessInstanceEntity processInstance,
        ActivityInstanceEntity fromActivityInstance,
        string transitionId,
        TransitionTypeEnum transitionType,
        TransitionFlyingTypeEnum flyingType,
        ActivityResource activityResource,
        SQLBuilder kit);

    string GetNodeMediatedMessage();
}
