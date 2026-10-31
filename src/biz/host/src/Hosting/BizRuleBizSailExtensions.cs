using System;
using saiflow.engine.Client.Hosting;
using saiflow.hub.BusinessRule.Hosting;

namespace saiflow.biz.Hosting;

/// <summary>
/// Biz composition：启用内置规则引擎（转发 bizRule）。
/// </summary>
public static class BizRuleBizSailExtensions
{
    /// <summary>
    /// 注册 NRules(<c>ruleTypes</c>) + bindingsJson 两种内置引擎；之后仍可用 <c>UseRuleEngine</c> 扩展自定义 mode。
    /// </summary>
    public static ISailCollection UseBizRules(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseBizRuleEngines();
    }

    public static SailClientBuilder UseBizRules(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseBizRuleEngines();
    }
}
