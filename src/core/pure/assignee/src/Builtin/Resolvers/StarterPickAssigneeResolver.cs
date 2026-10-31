using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 发起人自选：与 Specific 相同，吃 NextActivityPerformers；required=true 时无数据返回空列表由上层决定。
/// 禁止 sealed。
/// </summary>
public class StarterPickAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.StarterPick;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        var map = ctx.NextActivityPerformers;
        var activityId = ctx.ActivityId;
        if (map == null || string.IsNullOrEmpty(activityId) || !map.TryGetValue(activityId, out var performers)
            || performers == null)
            return list;

        foreach (var p in performers)
            list.Add(p);
        return list;
    }
}

/// <summary>审批人自选：语义同 StarterPick（下一跳由办理页写入）。禁止 sealed。</summary>
public class ApproverPickAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.ApproverPick;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx) =>
        new StarterPickAssigneeResolver().Resolve(ctx);
}
