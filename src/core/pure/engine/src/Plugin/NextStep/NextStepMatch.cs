namespace sailflow.engine.NextStep;

/// <summary>下一跳管道 Match 模式（对齐 Gate/Hook Whitelist|All）。</summary>
public enum NextStepMatch
{
    /// <summary>须配置 ProcessCodes/Ids/Patterns 之一，否则不命中。</summary>
    Whitelist = 0,

    /// <summary>不限制流程维（仍可受 ActivityCodes 约束）。</summary>
    All = 1
}
