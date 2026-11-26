using sailflow.utils;

namespace sailflow.assignee.Builtin;

/// <summary>
/// Mode=Pick（及 Preselect 回落入参）时从请求 <c>NextActivityPerformers</c> 取人。
/// 非 Scope 注册键；勿与旧 ApproverPick/StarterPick Type 混淆。
/// </summary>
public static class PickInputPerformers
{
    public static PerformerList FromRequest(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        if (ctx == null)
            return list;

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
