using System;
using System.Collections.Generic;
using sailflow.engine.Rules;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// 规则引擎注册（引擎只规定契约，实现由业务侧提供）。
/// </summary>
public static class SailRuleEngineExtensions
{
    /// <summary>追加一个 <see cref="IRuleEngine"/>（可多次，按 mode 扩展）；Build 时挂到 <see cref="SailClient"/>。</summary>
    public static ISailCollection AddRuleEngine(this ISailCollection services, IRuleEngine engine)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(engine);
        GetOrCreateList(services).Add(engine);
        return services;
    }

    internal static List<IRuleEngine> GetOrCreateList(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.RuleEngines, out var box)
            && box is List<IRuleEngine> list)
            return list;

        list = new List<IRuleEngine>();
        services.Properties[SailBuilderKeys.RuleEngines] = list;
        return list;
    }
}
