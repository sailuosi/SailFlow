using sailflow.data;
using sailflow.engine.Assignee;
using sailflow.engine.Client;
using sailflow.engine.Xpdl;
using mooSQL.data;

namespace sailflow.assignee.Adapt;

/// <summary>
/// 从流程定义 Activity 读取显式 BPMN ConfigPair（优先于 Legacy 推断）。
/// 无显式配置时返回 null，由 Pipeline 继续 Legacy / MultiSign 适配。
/// </summary>
public static class BpmnAssigneeConfigReader
{
    public static ConfigPair? ReadResolve(SailClient? sail, AssigneeInvokeRequest request)
    {
        var activity = TryLoadActivity(
            sail,
            request?.Kit,
            request?.ProcessId ?? request?.Runner?.ProcessId,
            request?.Version ?? request?.Runner?.Version,
            request?.ActivityId,
            processInstanceId: request?.ProcessInstanceId);
        if (activity == null)
            return null;

        // 仅显式 sf:assigneeResolve；旧 Performers/runner 仍走 LegacyAssigneeConfigInfer
        return ConfigPairJson.FromTypeAndParamsJson(
            activity.AssigneeResolveType,
            activity.AssigneeResolveParams);
    }

    public static ConfigPair? ReadLeave(SailClient? sail, LeaveInvokeRequest request)
    {
        var main = request?.MainActivityInstance;
        if (main == null)
            return null;

        // 1) AI 快照优先（加签子堆栈改写后自然生效）
        var fromAi = ConfigPairJson.FromTypeAndParamsJson(
            main.LeaveJudgeType,
            main.LeaveJudgeParams);
        if (fromAi != null)
            return fromAi;

        var activity = TryLoadActivity(
            sail,
            request.Kit,
            request.ProcessId ?? main.ProcessId,
            request.Version ?? request.Runner?.Version,
            main.ActivityId,
            processInstanceId: !string.IsNullOrWhiteSpace(request.ProcessInstanceId)
                ? request.ProcessInstanceId
                : main.ProcessInstanceId);
        if (activity == null)
            return null;

        // 2) BPMN 设计侧
        var pair = ConfigPairJson.FromTypeAndParamsJson(
            activity.LeaveJudgeType,
            activity.LeaveJudgeParams);
        if (pair != null)
            return pair;

        // 3) 无显式 LeaveJudge：旧 MultiSignDetail → Collaborate 等
        return LegacyMultiSignAdapter.InferLeaveFromActivity(activity);
    }

    private static Activity? TryLoadActivity(
        SailClient? sail,
        SQLBuilder? kit,
        string? processId,
        string? version,
        string? activityId,
        string processInstanceId)
    {
        if (sail == null || string.IsNullOrEmpty(activityId))
            return null;

        try
        {
            // 同事务优先复用 kit，禁止另开连接读未提交的流程实例
            var db = kit?.useSQL() ?? sail.UseRunSQL();

            if (string.IsNullOrEmpty(version) && !string.IsNullOrWhiteSpace(processInstanceId))
            {
                var pi = db.findRowById<ProcessInstanceEntity>(processInstanceId);
                version = pi?.Version;
                processId ??= pi?.ProcessId;
            }

            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version))
                return null;

            var model = sail.ProcessModelFactory.CreateByProcess(db, processId, version);
            return model?.GetActivity(activityId);
        }
        catch
        {
            return null;
        }
    }
}
