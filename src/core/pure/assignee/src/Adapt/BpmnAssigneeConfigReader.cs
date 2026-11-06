using saiflow.data;
using saiflow.engine.Assignee;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Xpdl;

namespace saiflow.assignee.Adapt;

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

        var activity = TryLoadActivity(
            sail,
            request.ProcessId ?? main.ProcessId,
            request.Version ?? request.Runner?.Version,
            main.ActivityId,
            processInstanceId: request.ProcessInstanceId > 0
                ? request.ProcessInstanceId
                : main.ProcessInstanceId);
        if (activity == null)
            return null;

        var pair = ConfigPairJson.FromTypeAndParamsJson(
            activity.LeaveJudgeType,
            activity.LeaveJudgeParams);
        if (pair != null)
            return pair;

        // 无显式 LeaveJudge：旧 MultiSignDetail → Collaborate 等
        return LegacyMultiSignAdapter.InferLeaveFromActivity(activity);
    }

    private static Activity? TryLoadActivity(
        SailClient? sail,
        string? processId,
        string? version,
        string? activityId,
        int? processInstanceId)
    {
        if (sail == null || string.IsNullOrEmpty(activityId))
            return null;

        try
        {
            if (string.IsNullOrEmpty(version) && processInstanceId is > 0)
            {
                var pi = sail.UseRunSQL().useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId.Value);
                version = pi?.Version;
                processId ??= pi?.ProcessId;
            }

            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version))
                return null;

            var model = sail.ProcessModelFactory.CreateByProcess(processId, version);
            return model?.GetActivity(activityId);
        }
        catch
        {
            return null;
        }
    }
}
