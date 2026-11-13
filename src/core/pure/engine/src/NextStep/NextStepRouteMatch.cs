using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace sailflow.engine.NextStep;

internal static class NextStepRouteMatch
{
    internal static bool Matches(
        NextStepRouteOptions options,
        string processCode,
        string processId,
        string activityCode)
    {
        if (options == null)
            return true;

        if (!MatchesProcess(options, processCode, processId))
            return false;

        return MatchesActivity(options.ActivityCodes, activityCode);
    }

    static bool MatchesProcess(NextStepRouteOptions options, string processCode, string processId)
    {
        if (options.Match == NextStepMatch.All)
            return true;

        var hasCodes = options.ProcessCodes?.Length > 0;
        var hasIds = options.ProcessIds?.Length > 0;
        var hasPatterns = options.ProcessCodePatterns?.Length > 0;
        if (!hasCodes && !hasIds && !hasPatterns)
            return false;

        if (hasCodes
            && !string.IsNullOrEmpty(processCode)
            && options.ProcessCodes.Contains(processCode, StringComparer.OrdinalIgnoreCase))
            return true;

        if (hasIds
            && !string.IsNullOrEmpty(processId)
            && options.ProcessIds.Contains(processId, StringComparer.OrdinalIgnoreCase))
            return true;

        if (hasPatterns && !string.IsNullOrEmpty(processCode))
        {
            foreach (var pattern in options.ProcessCodePatterns)
            {
                if (string.IsNullOrEmpty(pattern))
                    continue;
                try
                {
                    if (Regex.IsMatch(processCode, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                        return true;
                }
                catch (ArgumentException)
                {
                    // 非法正则：不命中该 pattern
                }
            }
        }

        return false;
    }

    static bool MatchesActivity(string[] activityCodes, string activityCode)
    {
        if (activityCodes == null || activityCodes.Length == 0)
            return true;
        if (string.IsNullOrEmpty(activityCode))
            return false;
        return activityCodes.Contains(activityCode, StringComparer.OrdinalIgnoreCase);
    }
}
