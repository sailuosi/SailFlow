using sailflow.engine.Assignee;
using sailflow.engine.Common;

namespace sailflow.assignee.Builtin;

/// <summary>抢办：子任务任一人同意即可。禁止 sealed。</summary>
public class RaceLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Race;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
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
