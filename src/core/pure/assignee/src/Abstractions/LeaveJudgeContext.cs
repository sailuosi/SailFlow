using saiflow.engine.Assignee;

namespace saiflow.assignee;

public class LeaveJudgeContext
{
    public LeaveInvokeRequest Request { get; set; } = null!;
    public ConfigPair Config { get; set; } = null!;
}
