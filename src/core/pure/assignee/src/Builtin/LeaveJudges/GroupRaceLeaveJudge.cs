using sailflow.engine.Assignee;

namespace sailflow.assignee.Builtin;

/// <summary>组内抢办 + 组间 AND/OR（Params.groupJoin）。禁止 sealed。</summary>
public class GroupRaceLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.GroupRace;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        var children = ctx.Request?.ChildActivityInstances;
        if (children == null || children.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "GroupRace needs MI children.");

        var groups = GroupLeaveHelper.GroupBy(children);
        return GroupLeaveHelper.CombineGroups(
            groups,
            GroupLeaveHelper.IsGroupRaceDone,
            GroupLeaveHelper.IsGroupRaceFailed,
            ctx.Config);
    }
}
