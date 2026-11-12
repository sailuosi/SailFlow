using sailflow.engine.Assignee;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 队长模式：Params.gateRoles 默认 Leader；组内关卡身份同意即组完成；组间 groupJoin。
/// 禁止 sealed。
/// </summary>
public class CaptainLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Captain;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        var children = ctx.Request?.ChildActivityInstances;
        if (children == null || children.Count == 0)
            return LeaveJudgeResult.Waiting("NoChildren", "Captain needs MI children.");

        var gateRoles = GroupLeaveHelper.ParseGateRoles(ctx.Config);
        var groups = GroupLeaveHelper.GroupBy(children);
        return GroupLeaveHelper.CombineGroups(
            groups,
            g => GroupLeaveHelper.IsCaptainGroupDone(g, gateRoles),
            g => GroupLeaveHelper.IsCaptainGroupFailed(g, gateRoles),
            ctx.Config);
    }
}
