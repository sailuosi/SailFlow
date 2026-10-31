using saiflow.engine.Assignee;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>按 GroupId 分组完成判定；空 GroupId 归入同一默认组。</summary>
public static class GroupLeaveHelper
{
    public const string DefaultGroupKey = "_";

    public static string GroupKeyOf(ActivityInstanceEntity child) =>
        string.IsNullOrWhiteSpace(child?.GroupId) ? DefaultGroupKey : child.GroupId.Trim();

    public static IReadOnlyDictionary<string, List<ActivityInstanceEntity>> GroupBy(
        IList<ActivityInstanceEntity> children)
    {
        var map = new Dictionary<string, List<ActivityInstanceEntity>>(StringComparer.OrdinalIgnoreCase);
        if (children == null)
            return map;
        foreach (var c in children)
        {
            var key = GroupKeyOf(c);
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<ActivityInstanceEntity>();
                map[key] = list;
            }
            list.Add(c);
        }
        return map;
    }

    public static bool IsGroupJoinAnd(ConfigPair? config)
    {
        var join = ConfigPairParams.GetString(config, "groupJoin", "And") ?? "And";
        return !string.Equals(join, "Or", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>组内抢办：组内任一人 Agreed → 组完成。</summary>
    public static bool IsGroupRaceDone(IList<ActivityInstanceEntity> group)
    {
        if (group == null || group.Count == 0)
            return false;
        return group.Any(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
    }

    public static bool IsGroupRaceFailed(IList<ActivityInstanceEntity> group)
    {
        if (group == null || group.Count == 0)
            return true;
        return group.All(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);
    }

    /// <summary>组内协办：组内按人数或比例（缺省全员同意）。</summary>
    public static bool IsGroupCollaborateDone(IList<ActivityInstanceEntity> group, ConfigPair? config)
    {
        if (group == null || group.Count == 0)
            return false;
        var agreed = group.Count(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
        var count = ConfigPairParams.GetFloat(config, "completeCount");
        var rate = ConfigPairParams.GetFloat(config, "completeRate");
        if (count != null)
            return agreed >= count.Value;
        if (rate != null)
            return agreed * 1.0 / group.Count >= rate.Value;
        return agreed >= group.Count;
    }

    public static bool IsGroupCollaborateFailed(IList<ActivityInstanceEntity> group, ConfigPair? config)
    {
        if (group == null || group.Count == 0)
            return true;
        var refused = group.Count(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);
        var count = ConfigPairParams.GetFloat(config, "completeCount");
        var rate = ConfigPairParams.GetFloat(config, "completeRate");
        float need;
        if (count != null)
            need = count.Value;
        else if (rate != null)
            need = (float)Math.Ceiling(rate.Value * group.Count);
        else
            need = group.Count;
        return refused > group.Count - need;
    }

    /// <summary>队长：关卡身份（默认 Leader）任一人同意 → 组完成。</summary>
    public static bool IsCaptainGroupDone(IList<ActivityInstanceEntity> group, string[] gateRoles)
    {
        if (group == null || group.Count == 0)
            return false;
        var roles = gateRoles is { Length: > 0 } ? gateRoles : new[] { GroupRoles.Leader };
        var gates = group.Where(a => RoleMatches(a.GroupRole, roles)).ToList();
        if (gates.Count == 0)
        {
            // 无关卡身份时回落：任一人同意（避免卡死）
            return group.Any(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
        }
        return gates.Any(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
    }

    public static bool IsCaptainGroupFailed(IList<ActivityInstanceEntity> group, string[] gateRoles)
    {
        if (group == null || group.Count == 0)
            return true;
        var roles = gateRoles is { Length: > 0 } ? gateRoles : new[] { GroupRoles.Leader };
        var gates = group.Where(a => RoleMatches(a.GroupRole, roles)).ToList();
        if (gates.Count == 0)
            return group.All(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);
        return gates.All(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);
    }

    public static string[] ParseGateRoles(ConfigPair? config)
    {
        var arr = ConfigPairParams.GetStringArray(config, "gateRoles");
        return arr.Length > 0 ? arr : new[] { GroupRoles.Leader };
    }

    public static LeaveJudgeResult CombineGroups(
        IReadOnlyDictionary<string, List<ActivityInstanceEntity>> groups,
        Func<List<ActivityInstanceEntity>, bool> isDone,
        Func<List<ActivityInstanceEntity>, bool> isFailed,
        bool joinAnd)
    {
        if (groups.Count == 0)
            return LeaveJudgeResult.Waiting("NoGroups", "No grouped children.");

        var doneCount = 0;
        var failedCount = 0;
        foreach (var g in groups.Values)
        {
            if (isDone(g))
                doneCount++;
            else if (isFailed(g))
                failedCount++;
        }

        if (joinAnd)
        {
            if (doneCount == groups.Count)
                return LeaveJudgeResult.Passed("GroupAnd", "All groups completed.");
            if (failedCount > 0)
                return LeaveJudgeResult.NotPassed("GroupAndFailed", "At least one group failed.");
            return LeaveJudgeResult.Waiting("GroupAndPending", $"Done {doneCount}/{groups.Count} groups.");
        }

        if (doneCount > 0)
            return LeaveJudgeResult.Passed("GroupOr", "At least one group completed.");
        if (failedCount == groups.Count)
            return LeaveJudgeResult.NotPassed("GroupOrFailed", "All groups failed.");
        return LeaveJudgeResult.Waiting("GroupOrPending", "No group completed yet.");
    }

    private static bool RoleMatches(string? role, string[] gateRoles)
    {
        var r = string.IsNullOrWhiteSpace(role) ? GroupRoles.Member : role.Trim();
        return gateRoles.Any(g => string.Equals(g, r, StringComparison.OrdinalIgnoreCase));
    }
}
