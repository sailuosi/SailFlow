using System;
using System.Linq;

namespace saiflow.engine.Hooks
{
    internal static class WorkflowCapabilityMatch
    {
        internal static bool MatchesProcess(
            HookMatch match,
            string[] processCodes,
            string[] processIds,
            string processCode,
            string processId)
        {
            if (match == HookMatch.All)
                return true;

            var hasFilter = (processCodes?.Length ?? 0) > 0 || (processIds?.Length ?? 0) > 0;
            if (!hasFilter)
                return false;

            if (processCodes?.Length > 0
                && !string.IsNullOrEmpty(processCode)
                && processCodes.Contains(processCode, StringComparer.OrdinalIgnoreCase))
                return true;

            if (processIds?.Length > 0
                && !string.IsNullOrEmpty(processId)
                && processIds.Contains(processId, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        internal static bool MatchesActivity(string[] activityCodes, string activityCode)
        {
            if (activityCodes == null || activityCodes.Length == 0)
                return true;
            if (string.IsNullOrEmpty(activityCode))
                return false;
            return activityCodes.Contains(activityCode, StringComparer.OrdinalIgnoreCase);
        }
    }
}
