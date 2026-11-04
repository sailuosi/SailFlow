using saiflow.engine.Common;
using saiflow.engine.Xpdl;

namespace saiflow.assignee.Adapt;

/// <summary>
/// 旧 sf:Performers / sf:MultiSignDetail → ConfigPair（无显式 assigneeResolve/leaveJudge 时）。
/// </summary>
public static class LegacyMultiSignAdapter
{
    /// <summary>
    /// 节点定义了角色类 Partaker → RoleGroup；否则 null（Specific 等依赖 runner）。
    /// </summary>
    public static ConfigPair? InferResolveFromActivity(Activity? activity)
    {
        if (activity?.PartakerList == null || activity.PartakerList.Count == 0)
            return null;

        var roleType = PartakerTypeEnum.Role.ToString();
        var roleIds = activity.PartakerList
            .Where(p => p != null
                        && !string.IsNullOrEmpty(p.OuterId)
                        && string.Equals(p.OuterType, roleType, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.OuterId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roleIds.Length == 0)
            return null;

        return ConfigPair.Create(AssigneeResolveTypeCodes.RoleGroup, new Dictionary<string, object?>
        {
            ["roleIds"] = roleIds
        });
    }

    /// <summary>
    /// MultiSignDetail.SignTogether → Collaborate（带 completeCount/completeRate）。
    /// </summary>
    public static ConfigPair? InferLeaveFromActivity(Activity? activity)
    {
        var ms = activity?.MultiSignDetail;
        if (ms == null || ms.ComplexType != ComplexTypeEnum.SignTogether)
            return null;

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (ms.CompleteOrder != null)
        {
            if (ms.CompareType == CompareTypeEnum.Percentage)
                parameters["completeRate"] = ms.CompleteOrder.Value;
            else
                parameters["completeCount"] = ms.CompleteOrder.Value;
        }

        return ConfigPair.Create(LeaveJudgeTypeCodes.Collaborate, parameters);
    }
}
