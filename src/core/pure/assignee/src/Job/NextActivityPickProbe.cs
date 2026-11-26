using sailflow.assignee;
using sailflow.hosting;
using sailflow.utils;
using sailflow.xml;

namespace sailflow.assignee.Job;

/// <summary>
/// 遗留：曾用于 AutoApprove 入队前预判下一跳选人。新产品改为发送主路径校验，不再作门禁。
/// </summary>
[Obsolete("Auto-approve no longer pre-checks next pick; RunProcess validates instead.")]
public static class NextActivityPickProbe
{
    public const string ExtraIncompleteKey = "nextPerformersIncomplete";
    public const string ExtraReasonKey = "nextPerformersReason";

    /// <returns>true = 应禁止自动代办</returns>
    public static bool RequiresManualPick(
        SailClient sail,
        string? processId,
        string? version,
        string? fromActivityId,
        out string reason)
    {
        reason = "";
        if (sail == null
            || string.IsNullOrWhiteSpace(processId)
            || string.IsNullOrWhiteSpace(version)
            || string.IsNullOrWhiteSpace(fromActivityId))
            return false;

        try
        {
            var model = sail.ProcessModelFactory.CreateByProcess(processId, version);
            var next = model?.GetNextActivityListWithoutCondition(fromActivityId);
            if (next == null || next.Count == 0)
                return false;

            var userNodes = next
                .Where(a => a != null
                    && (a.ActivityType == ActivityTypeEnum.TaskNode
                        || a.ActivityType == ActivityTypeEnum.MultiSignNode))
                .ToList();

            if (userNodes.Count > 1)
            {
                reason = "multiple next user tasks without specified outlet";
                return true;
            }

            if (userNodes.Count == 1)
            {
                var node = userNodes[0];
                if (AssigneeSelectModelCodes.TryNormalize(node.AssigneeSelectModel, out var mode)
                    && string.Equals(mode, AssigneeSelectModelCodes.Pick, StringComparison.OrdinalIgnoreCase))
                {
                    reason = "next selectModel is Pick";
                    return true;
                }

                if (node.ActivityType == ActivityTypeEnum.MultiSignNode)
                {
                    reason = "next is MultiSign (SignTogether needs real performers)";
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static IDictionary<string, string> MergeIncompleteExtra(
        IDictionary<string, string>? existing,
        string reason)
    {
        var extra = existing != null
            ? new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        extra[ExtraIncompleteKey] = "1";
        if (!string.IsNullOrWhiteSpace(reason))
            extra[ExtraReasonKey] = reason;
        return extra;
    }
}
