using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;

namespace saiflow.assignee.Job;

/// <summary>
/// §11.3：探测下一跳是否要求运行时选人 / 多出口人工（供打锚点 Extra 与 Worker 共用）。
/// </summary>
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
                var t = userNodes[0].AssigneeResolveType?.Trim();
                if (string.Equals(t, "ApproverPick", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "StarterPick", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "next Resolve is " + t;
                    return true;
                }

                if (userNodes[0].ActivityType == ActivityTypeEnum.MultiSignNode)
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
