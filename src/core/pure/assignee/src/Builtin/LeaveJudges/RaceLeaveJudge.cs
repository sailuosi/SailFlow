using saiflow.engine.Assignee;

namespace saiflow.assignee.Builtin;

/// <summary>抢办占位（S0）。禁止 sealed。</summary>
public class RaceLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Race;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        return LeaveJudgeResult.Waiting("S0_Placeholder", "Race leave judge placeholder; use legacy path until S1.");
    }
}
