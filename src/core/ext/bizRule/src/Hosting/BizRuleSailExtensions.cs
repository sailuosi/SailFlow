using System;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.engine.Rules;
using sailflow.hub.BusinessRule;

namespace sailflow.hub.Hosting
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
            builder.AddRuleEngine(new NRulesRuleTypesEngine());
            builder.AddRuleEngine(new BindingsJsonRuleEngine());
            return builder;
        }

        /// <summary>挂载两种内置引擎到服务集合。</summary>
        public static ISailCollection UseBizRuleEngines(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddRuleEngine(new NRulesRuleTypesEngine());
            services.AddRuleEngine(new BindingsJsonRuleEngine());
            return services;
        }

        /// <summary>在已构建的 Client 上挂载内置引擎（可再 addRuleEngine 覆盖/追加自定义 mode）。</summary>
        public static SailClient UseBizRuleEngines(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.addRuleEngine(new NRulesRuleTypesEngine());
            client.addRuleEngine(new BindingsJsonRuleEngine());
            return client;
        }
    }
}
