using saiflow.engine.Assignee;

namespace saiflow.assignee;

/// <summary>时刻 B：按 Type 判定能否流出。内置实现禁止 sealed。</summary>
public interface IActivityLeaveJudge
{
    string Type { get; }

    LeaveJudgeResult CanLeave(LeaveJudgeContext ctx);
}
