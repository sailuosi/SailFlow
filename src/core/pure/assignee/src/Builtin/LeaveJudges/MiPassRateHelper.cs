using saiflow.engine.Assignee;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;
using saiflow.engine.Xpdl.Common;

namespace saiflow.assignee.Builtin;

/// <summary>会签通过率计算（对齐 ActivityInstanceManager.CheckMIPassRateInfo）。</summary>
public static class MiPassRateHelper
{
    public static LeaveJudgeResult Evaluate(
        ActivityInstanceEntity main,
        IList<ActivityInstanceEntity> children,
        float? completeCountOrRateOverride = null,
        string? compareOverride = null)
    {
        if (main == null || children == null || children.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "No MI children yet.");

        var agreedCount = children.Count(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
        var refusedCount = children.Count(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);

        var compareType = ResolveCompareType(main, compareOverride);
        var threshold = completeCountOrRateOverride ?? main.CompleteOrder;
        if (threshold == null)
            return LeaveJudgeResult.Waiting("NoThreshold", "CompleteOrder / completeCount|Rate missing.");

        var merge = main.MergeType ?? (short)MergeTypeEnum.Parallel;

        if (merge == (short)MergeTypeEnum.Sequence)
            return EvaluateCore(agreedCount, refusedCount, children, threshold.Value, compareType, sequence: true);
        if (merge == (short)MergeTypeEnum.Parallel)
            return EvaluateCore(agreedCount, refusedCount, children, threshold.Value, compareType, sequence: false);

        return LeaveJudgeResult.Waiting("UnknownMerge", $"Unknown MergeType={merge}");
    }

    /// <summary>抢办：任一人同意即 Passed。</summary>
    public static LeaveJudgeResult EvaluateRace(IList<ActivityInstanceEntity> children)
    {
        if (children == null || children.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "No children for race.");

        var agreed = children.Count(a => a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed);
        if (agreed >= 1)
            return LeaveJudgeResult.Passed("Race", "At least one agreed.");

        var allSettled = children.All(a =>
            a.ApprovalStatus == (short)ApprovalStatusEnum.Agreed
            || a.ApprovalStatus == (short)ApprovalStatusEnum.Refused);
        if (allSettled)
            return LeaveJudgeResult.NotPassed("RaceAllRefused", "All settled without agree.");

        return LeaveJudgeResult.Waiting("RacePending", "Waiting for first agree.");
    }

    private static short ResolveCompareType(ActivityInstanceEntity main, string? compareOverride)
    {
        if (!string.IsNullOrEmpty(compareOverride))
        {
            if (string.Equals(compareOverride, "Percentage", StringComparison.OrdinalIgnoreCase)
                || string.Equals(compareOverride, "Rate", StringComparison.OrdinalIgnoreCase))
                return (short)CompareTypeEnum.Percentage;
            return (short)CompareTypeEnum.Count;
        }

        return main.CompareType ?? (short)CompareTypeEnum.Count;
    }

    private static LeaveJudgeResult EvaluateCore(
        int agreedCount,
        int refusedCount,
        IList<ActivityInstanceEntity> children,
        float threshold,
        short compareType,
        bool sequence)
    {
        if (compareType == (short)CompareTypeEnum.Count)
        {
            float allCount = sequence
                ? children.Max(a => a.CompleteOrder ?? 0)
                : children.Count;
            if (allCount <= 0)
                allCount = children.Count;
            var negativeCount = allCount - threshold;

            if (refusedCount > negativeCount)
                return LeaveJudgeResult.NotPassed("CountRefused", "Refused exceeded allowance.");
            if (agreedCount >= threshold)
                return LeaveJudgeResult.Passed("CountReached", "Agreed count reached threshold.");
            return LeaveJudgeResult.Waiting("CountPending", "Need more approvals.");
        }

        if (compareType == (short)CompareTypeEnum.Percentage)
        {
            var allCount = children.Count;
            if (allCount == 0)
                return LeaveJudgeResult.Waiting("NoChildren", "Empty children.");
            var negativePercentage = 1 - threshold;
            if ((refusedCount * 1.0) / allCount > negativePercentage)
                return LeaveJudgeResult.NotPassed("RateRefused", "Refuse rate exceeded.");
            if ((agreedCount * 1.0) / allCount >= threshold)
                return LeaveJudgeResult.Passed("RateReached", "Agree rate reached threshold.");
            return LeaveJudgeResult.Waiting("RatePending", "Need more approvals.");
        }

        return LeaveJudgeResult.Waiting("UnknownCompare", $"Unknown CompareType={compareType}");
    }
}
