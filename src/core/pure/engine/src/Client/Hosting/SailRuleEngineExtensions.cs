using System;
using System.Collections.Generic;
using saiflow.engine.Rules;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

/// <summary>
/// 规则引擎注册（引擎只规定契约，实现由业务侧提供）。
/// </summary>
public static class SailRuleEngineExtensions
{
    /// <summary>登记一个 <see cref="IRuleEngine"/>；Build 时挂到 <see cref="SailClient"/>。</summary>
    public static ISailCollection UseRuleEngine(this ISailCollection services, IRuleEngine engine)
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
