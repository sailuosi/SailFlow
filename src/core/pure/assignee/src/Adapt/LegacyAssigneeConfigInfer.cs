using saiflow.engine.Assignee;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;

namespace saiflow.assignee.Adapt;

/// <summary>
/// 无显式 BPMN ConfigPair 时，从既有 runner / 会签主活动推断 Type（旧数据双轨适配）。
/// </summary>
public static class LegacyAssigneeConfigInfer
{
    public static ConfigPair? InferResolve(AssigneeInvokeRequest request)
    {
        if (request?.Runner == null)
            return null;

        var runner = request.Runner;
        var activityId = request.ActivityId;

        // R2：仅适配「前端已带 Specific 名单」或测试 Single；禁止 Definition→RoleGroup 运行时静默回落
        //（角色须在 BPMN 显式写 AssigneeResolve Type=RoleGroup）
        if (runner.NextPerformerType == NextPerformerIntTypeEnum.Specific)
        {
            if (HasPerformers(request, activityId))
                return ConfigPair.Create(AssigneeResolveTypeCodes.Specific);
            return null;
        }

        if (runner.NextPerformerType == NextPerformerIntTypeEnum.Single)
        {
            // 测试向：当前操作人 → 当作 FixedUsers 单人
            return ConfigPair.Create(AssigneeResolveTypeCodes.FixedUsers, new Dictionary<string, object?>
            {
                ["userIds"] = runner.UserId ?? "",
                ["userNames"] = runner.UserName ?? runner.UserId ?? ""
            });
        }

        return null;
    }

    public static ConfigPair? InferLeave(LeaveInvokeRequest request)
    {
        var main = request?.MainActivityInstance;
        if (main == null)
            return null;

        // 会签主节点：有 MergeType / CompleteOrder → Collaborate
        if (main.ComplexType == (short)ComplexTypeEnum.SignTogether
            && main.MergeType != null
            && main.CompleteOrder != null)
        {
            return ConfigPair.Create(LeaveJudgeTypeCodes.Collaborate);
        }

        return null;
    }

    private static bool HasPerformers(AssigneeInvokeRequest request, string activityId)
    {
        var map = request.NextActivityPerformers;
        return map != null
               && !string.IsNullOrEmpty(activityId)
               && map.TryGetValue(activityId, out var list)
               && list != null
               && list.Count > 0;
    }
}
