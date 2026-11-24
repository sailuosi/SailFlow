using sailflow.assignee;
using sailflow.data;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;

namespace sailflow.assignee.Builtin;

/// <summary>会签/协办通过率：优先 <see cref="LeaveSeatSnapshot"/> EffectiveWeight；无 Seats 时回落子 AI 人头。</summary>
public static class MiPassRateHelper
{
    public static LeaveJudgeResult Evaluate(
        ActivityInstanceEntity main,
        IList<ActivityInstanceEntity> children,
        float? completeCountOrRateOverride = null,
        string? compareOverride = null)
        => Evaluate(main, children, seats: null, completeCountOrRateOverride, compareOverride);

    public static LeaveJudgeResult Evaluate(
        ActivityInstanceEntity main,
        IList<ActivityInstanceEntity> children,
        IList<LeaveSeatSnapshot>? seats,
        float? completeCountOrRateOverride = null,
        string? compareOverride = null)
    {
        if (main == null)
            return LeaveJudgeResult.Waiting("NoMain", "MainActivityInstance required.");

        var seatList = seats != null && seats.Count > 0
            ? seats.ToList()
            : sailflow.assignee.LeaveSeatProjector.FromChildActivities(
                children ?? Array.Empty<ActivityInstanceEntity>());

        if (seatList.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "No MI seats yet.");

        var compareType = ResolveCompareType(main, compareOverride);
        var threshold = completeCountOrRateOverride ?? main.CompleteOrder;
        if (threshold == null)
            return LeaveJudgeResult.Waiting("NoThreshold", "CompleteOrder / completeCount|Rate missing.");

        var merge = main.MergeType ?? (short)MergeTypeEnum.Parallel;
        var sequence = merge == (short)MergeTypeEnum.Sequence;

        var agreed = EffectiveWeightHelper.SumAgreedEffective(seatList);
        var refused = EffectiveWeightHelper.SumRefusedEffective(seatList);
        var denominator = EffectiveWeightHelper.SumDenominator(seatList);

        if (merge != (short)MergeTypeEnum.Sequence && merge != (short)MergeTypeEnum.Parallel)
            return LeaveJudgeResult.Waiting("UnknownMerge", $"Unknown MergeType={merge}");

        return EvaluateWeighted(agreed, refused, denominator, seatList, threshold.Value, compareType, sequence);
    }

    /// <summary>抢办：任一有效同意即 Passed。</summary>
    public static LeaveJudgeResult EvaluateRace(IList<ActivityInstanceEntity> children)
        => EvaluateRace(sailflow.assignee.LeaveSeatProjector.FromChildActivities(
            children ?? Array.Empty<ActivityInstanceEntity>()));

    public static LeaveJudgeResult EvaluateRace(IList<LeaveSeatSnapshot>? seats)
    {
        var seatList = seats?.ToList() ?? new List<LeaveSeatSnapshot>();
        if (seatList.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "No seats for race.");

        if (EffectiveWeightHelper.SumAgreedEffective(seatList) > 0)
            return LeaveJudgeResult.Passed("Race", "At least one agreed.");

        var openOrPending = seatList.Any(s =>
            s != null
            && !s.ClosedOrCancelled
            && !s.Agreed
            && !s.Refused);
        if (!openOrPending)
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

    private static LeaveJudgeResult EvaluateWeighted(
        decimal agreed,
        decimal refused,
        decimal denominator,
        IList<LeaveSeatSnapshot> seats,
        float threshold,
        short compareType,
        bool sequence)
    {
        if (compareType == (short)CompareTypeEnum.Count)
        {
            float allCount = sequence
                ? (float)Math.Max((double)denominator, seats.Count)
                : (float)(denominator > 0 ? denominator : seats.Count);
            if (allCount <= 0)
                allCount = seats.Count;
            var negativeCount = allCount - threshold;

            if ((float)refused > negativeCount)
                return LeaveJudgeResult.NotPassed("CountRefused", "Refused exceeded allowance.");
            if ((float)agreed >= threshold)
                return LeaveJudgeResult.Passed("CountReached", "Agreed weight reached threshold.");
            return LeaveJudgeResult.Waiting("CountPending", "Need more approvals.");
        }

        if (compareType == (short)CompareTypeEnum.Percentage)
        {
            var all = denominator > 0 ? denominator : seats.Count;
            if (all <= 0)
                return LeaveJudgeResult.Waiting("NoChildren", "Empty seats.");
            var negativePercentage = 1 - threshold;
            if ((double)refused / (double)all > negativePercentage)
                return LeaveJudgeResult.NotPassed("RateRefused", "Refuse rate exceeded.");
            if ((double)agreed / (double)all >= threshold)
                return LeaveJudgeResult.Passed("RateReached", "Agree rate reached threshold.");
            return LeaveJudgeResult.Waiting("RatePending", "Need more approvals.");
        }

        return LeaveJudgeResult.Waiting("UnknownCompare", $"Unknown CompareType={compareType}");
    }
}
