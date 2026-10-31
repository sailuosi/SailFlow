using System;

namespace saiflow.engine.Common
{
    /// <summary>
    /// 内置 mode 的枚举便利值（自定义引擎可只用字符串 mode，不必进本枚举）。
    /// 持久化字符串见 <see cref="RuleSetModeHelper"/>。
    /// </summary>
    public enum RuleSetModeEnum
    {
        /// <summary>内置：NRules Fluent（bizRule <c>NRulesRuleTypesEngine</c>）。</summary>
        RuleTypes = 0,

        /// <summary>内置：JSON DSL（bizRule <c>BindingsJsonRuleEngine</c>）。</summary>
        BindingsJson = 1
    }

    /// <summary>内置 mode 与 <c>wf_rule_set.mode</c> 字符串约定。</summary>
    public static class RuleSetModeHelper
    {
        public const string PersistedRuleTypes = "ruleTypes";
        public const string PersistedBindingsJson = "bindingsJson";

        /// <summary>Empty or whitespace is treated as <see cref="RuleSetModeEnum.RuleTypes"/>.</summary>
        public static bool TryParseFromPersisted(string persisted, out RuleSetModeEnum mode)
        {
            mode = RuleSetModeEnum.RuleTypes;
            if (string.IsNullOrWhiteSpace(persisted))
                return true;

            var s = persisted.Trim();
            if (string.Equals(s, PersistedBindingsJson, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuleSetModeEnum.BindingsJson;
                return true;
            }

            if (string.Equals(s, PersistedRuleTypes, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuleSetModeEnum.RuleTypes;
                return true;
            }

            return false;
        }

        public static string ToPersistedString(RuleSetModeEnum mode) =>
            mode == RuleSetModeEnum.BindingsJson ? PersistedBindingsJson : PersistedRuleTypes;
    }
}
