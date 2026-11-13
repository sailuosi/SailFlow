using System.Collections.Generic;

namespace sailflow.engine.NextStep;

/// <summary>推荐基类：冲刷静态袋 + Contribute。</summary>
public abstract class NextStepPrepare : INextStepPrepare
{
    private readonly Dictionary<string, string> _literals =
        new(System.StringComparer.OrdinalIgnoreCase);

    public virtual bool IsMatch(NextStepRouteContext ctx) => true;

    public virtual void Prepare(NextStepRouteContext ctx)
    {
        if (ctx?.Conditions == null)
            return;
        foreach (var kv in _literals)
            ctx.Conditions[kv.Key] = kv.Value ?? string.Empty;
        Contribute(ctx);
    }

    /// <summary>动态补键。默认空。</summary>
    protected virtual void Contribute(NextStepRouteContext ctx) { }

    /// <summary>构造期声明静态键。</summary>
    protected void IfCondition(string name, string value)
    {
        if (string.IsNullOrEmpty(name))
            return;
        _literals[name] = value ?? string.Empty;
    }
}
