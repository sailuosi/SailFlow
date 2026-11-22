namespace sailflow.biz.Approval;

/// <summary>终态映射结果：Finish / Abort / 非终态忽略。</summary>
public enum SailApprovalLifecycleKind
{
    None = 0,
    Finish = 1,
    Abort = 2
}

/// <summary>事件 + ProcessState 双读后的门面分发决策。</summary>
public sealed class SailApprovalLifecycleDecision
{
    public SailApprovalLifecycleKind Kind { get; init; }

    /// <summary><see cref="ApprovalOutcomeCodes"/>；仅 Finish。</summary>
    public string? Outcome { get; init; }

    /// <summary>仅 Abort。</summary>
    public ApprovalAbortKind? AbortKind { get; init; }

    public static SailApprovalLifecycleDecision Ignore { get; } = new()
    {
        Kind = SailApprovalLifecycleKind.None
    };

    public static SailApprovalLifecycleDecision Finish(string outcome) => new()
    {
        Kind = SailApprovalLifecycleKind.Finish,
        Outcome = outcome
    };

    public static SailApprovalLifecycleDecision Abort(ApprovalAbortKind kind) => new()
    {
        Kind = SailApprovalLifecycleKind.Abort,
        AbortKind = kind
    };
}
