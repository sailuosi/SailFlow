namespace saiflow.assignee.Job;

/// <summary>R10：自动审批锚点 Job 选项。</summary>
public sealed class AutoApproveAnchorOptions
{
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 5;
    public int LeaseSeconds { get; set; } = 60;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>尚无 Waiting 任务时推迟重试。</summary>
    public TimeSpan RetryWhenNoTask { get; set; } = TimeSpan.FromSeconds(3);
    public bool AutoStart { get; set; }
}
