using sailflow.assignee;
using sailflow.assignee.Builtin;
using sailflow.engine.Assignee;

namespace sailflow.biz.Assignee;

/// <summary>
/// 同码覆写样例：仍用 Type=<c>Collaborate</c>，在内置协办前加租户钩子。
/// UseBizAssignees 后注册以覆盖内置。
/// </summary>
public class TenantCollaborateLeaveJudge : CollaborateLeaveJudge
{
    public override LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        // 样例钩子：Params.tenantRequireAll=true 时强制按人数=子实例总数
        if (ConfigPairParams.GetBool(ctx.Config, "tenantRequireAll"))
        {
            var children = ctx.Request?.ChildActivityInstances;
            var n = children?.Count ?? 0;
            if (n > 0)
            {
                var forced = ConfigPair.Create(LeaveJudgeTypeCodes.Collaborate, new Dictionary<string, object?>
                {
                    ["completeCount"] = n
                });
                return base.CanLeave(new LeaveJudgeContext
                {
                    Request = ctx.Request,
                    Config = forced
                });
            }
        }

        return base.CanLeave(ctx);
    }
}
