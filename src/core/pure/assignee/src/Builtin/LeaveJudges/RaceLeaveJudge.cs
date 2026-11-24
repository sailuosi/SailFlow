using sailflow.assignee;
using sailflow.engine.Common;

namespace sailflow.assignee.Builtin;

/// <summary>抢办：子任务任一人同意即可。禁止 sealed。</summary>
public class RaceLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Race;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        // 同级席位（直属 Task ∪ 直属子 AI）优先；扁平 Race 可无子 AI
        var seats = ctx.Request?.Seats;
        if (seats != null && seats.Count > 0)
            return MiPassRateHelper.EvaluateRace(seats);

        var children = ctx.Request?.ChildActivityInstances;
        if (children == null || children.Count == 0)
        {
            var current = ctx.Request?.CurrentActivityInstance;
            if (current?.ApprovalStatus == (short)ApprovalStatusEnum.Agreed)
                return LeaveJudgeResult.Passed("RaceSingle", "Current activity agreed.");
            return LeaveJudgeResult.Waiting("RaceNoChildren", "No children; waiting.");
        }

        return MiPassRateHelper.EvaluateRace(children);
    }
}
