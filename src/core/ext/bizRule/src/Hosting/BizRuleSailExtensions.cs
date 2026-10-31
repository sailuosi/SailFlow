using System;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using saiflow.engine.Rules;
using saiflow.hub.BusinessRule.Engines;

namespace saiflow.hub.BusinessRule.Hosting
{
    /// <summary>
    /// 注册 bizRule 内置规则引擎（NRules ruleTypes + bindingsJson），并允许继续 <see cref="IRuleEngine"/> 扩展。
    /// </summary>
    public static class BizRuleSailExtensions
    {
        /// <summary>
        /// 挂载两种内置引擎到 Builder / Services。
        /// </summary>
        public static SailClientBuilder UseBizRuleEngines(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.UseRuleEngine(new NRulesRuleTypesEngine());
            builder.UseRuleEngine(new BindingsJsonRuleEngine());
            return builder;
        }

        /// <summary>挂载两种内置引擎到服务集合。</summary>
        public static ISailCollection UseBizRuleEngines(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.UseRuleEngine(new NRulesRuleTypesEngine());
            services.UseRuleEngine(new BindingsJsonRuleEngine());
            return services;
        }

        /// <summary>在已构建的 Client 上挂载内置引擎（可再 useRuleEngine 覆盖/追加自定义 mode）。</summary>
        public static SailClient UseBizRuleEngines(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.useRuleEngine(new NRulesRuleTypesEngine());
            client.useRuleEngine(new BindingsJsonRuleEngine());
            return client;
        }
    }
}
