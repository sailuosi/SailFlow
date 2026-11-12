using sailflow.engine.Assignee;
using sailflow.engine.Entity;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 协办：按主活动 MergeType/CompareType/CompleteOrder，或 Params completeCount|completeRate。
/// 禁止 sealed。
/// </summary>
public class CollaborateLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Collaborate;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        var main = ctx.Request?.MainActivityInstance;
        var children = ctx.Request?.ChildActivityInstances;
        if (main == null)
            return LeaveJudgeResult.Waiting("NoMain", "MainActivityInstance required.");

        float? overrideThreshold = null;
        string? compareOverride = null;
        var count = ConfigPairParams.GetFloat(ctx.Config, "completeCount");
        var rate = ConfigPairParams.GetFloat(ctx.Config, "completeRate");
        if (count != null)
        {
            overrideThreshold = count;
            compareOverride = "Count";
        }
        else if (rate != null)
        {
            overrideThreshold = rate;
            compareOverride = "Percentage";
        }

        return MiPassRateHelper.Evaluate(
            main,
            children ?? Array.Empty<ActivityInstanceEntity>(),
            overrideThreshold,
            compareOverride);
    }
}
