namespace sailflow.engine.NextStep;

/// <summary>
/// Prepare / Filter / Evaluator 注册 Options（Match 正典，字段同源）。
/// </summary>
public sealed class NextStepRouteOptions
{
    public NextStepMatch Match { get; set; } = NextStepMatch.Whitelist;

    /// <summary>流程定义编码（精确，忽略大小写）。</summary>
    public string[] ProcessCodes { get; set; }

    /// <summary>流程定义 Id。</summary>
    public string[] ProcessIds { get; set; }

    /// <summary>流程编码正则；与 ProcessCodes 为或关系。</summary>
    public string[] ProcessCodePatterns { get; set; }

    /// <summary>当前节点 ActivityCode；空则不限节点。</summary>
    public string[] ActivityCodes { get; set; }

    /// <summary>执行序；未设时由注册序填充。</summary>
    public int? Order { get; set; }
}
