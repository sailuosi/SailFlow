using sailflow.engine.Assignee;

namespace sailflow.assignee.Builtin;

/// <summary>组内协办（人数/比例）+ 组间 AND/OR。禁止 sealed。</summary>
public class GroupCollaborateLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.GroupCollaborate;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        var children = ctx.Request?.ChildActivityInstances;
        if (children == null || children.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "GroupCollaborate needs MI children.");

        var groups = GroupLeaveHelper.GroupBy(children);
        var config = ctx.Config;
        return GroupLeaveHelper.CombineGroups(
            groups,
            g => GroupLeaveHelper.IsGroupCollaborateDone(g, config),
            g => GroupLeaveHelper.IsGroupCollaborateFailed(g, config),
            config);
    }
}
