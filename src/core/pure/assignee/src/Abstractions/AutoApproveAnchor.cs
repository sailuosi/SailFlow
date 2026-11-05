namespace saiflow.assignee;

/// <summary>
/// R10：自动审批意图锚点。时刻 A 只记录；发送成功后由普通 Job 苛刻复核并代办。
/// 主键语义：ProcessInstanceId + ActivityId（TaskId 可在消费时补齐）。
/// </summary>
public sealed class AutoApproveAnchor
{
    public string StrategyCode { get; init; } = "";
    public string? Reason { get; init; }
    public string? ProcessId { get; init; }
    public string? Version { get; init; }
    public int? ProcessInstanceId { get; init; }
    public string? ActivityId { get; init; }
    public string? ActivityCode { get; init; }
    /// <summary>可在 Job 侧由 Outbox 列覆盖（CreateNewTask 回写）。</summary>
    public int? ActivityInstanceId { get; set; }
    public int? TaskId { get; init; }
    /// <summary>可选：优先代办的用户编码；空则由 Job 解析 Waiting 任务办理人。</summary>
    public string? PreferUserId { get; init; }
    public string? PreferUserName { get; init; }
    public IDictionary<string, string>? Extra { get; init; }
}
