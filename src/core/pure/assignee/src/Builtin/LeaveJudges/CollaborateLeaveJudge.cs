using saiflow.engine.Assignee;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 协办完成判定占位（S0）。尚无 ConfigPair / 任务快照时返回未处理语义由 Pipeline 决定；
/// 有 Params 时可按 completeCount / completeRate 粗判（完整组语义在 S1/S2）。
/// 禁止 sealed。
/// </summary>
public class CollaborateLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => LeaveJudgeTypeCodes.Collaborate;

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        // S0：无足够快照时交回双轨（Pipeline 侧不调用本实现除非已显式配 Type）
        return LeaveJudgeResult.Waiting("S0_Placeholder", "Collaborate leave judge placeholder; use legacy MI until S1.");
    }
}
