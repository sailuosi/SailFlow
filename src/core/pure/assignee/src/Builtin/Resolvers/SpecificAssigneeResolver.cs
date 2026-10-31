using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>透传 runner / NextActivityPerformers 中已指定的办理人。禁止 sealed。</summary>
public class SpecificAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.Specific;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        var map = ctx.NextActivityPerformers;
        var activityId = ctx.ActivityId;
        if (map == null || string.IsNullOrEmpty(activityId) || !map.TryGetValue(activityId, out var performers)
            || performers == null)
        {
            return list;
        }

        foreach (var p in performers)
            list.Add(p);
        return list;
    }
}
