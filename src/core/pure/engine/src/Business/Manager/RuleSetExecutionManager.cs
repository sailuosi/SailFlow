using System;
using System.Collections.Generic;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Rules;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// 按 <c>wf_rule_set.mode</c> 分派到已注册的 <see cref="IRuleEngine"/>（实现由业务侧挂载）。
    /// </summary>
    public class RuleSetExecutionManager
    {
        private readonly SailClient _client;

        public RuleSetExecutionManager(SailClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public RuleExecutionResultEntity Execute(RuleSetEntity ruleSet, IDictionary<string, object> variables)
        {
            if (ruleSet == null || string.IsNullOrWhiteSpace(ruleSet.RuleSetCode))
            {
                return Fail("Rule set not found");
            }

            var modeKey = ResolveModeKey(ruleSet.Mode);
            if (!_client.RuleEngines.TryGet(modeKey, out var engine))
            {
                var known = _client.RuleEngines.Modes.Count == 0
                    ? "(none)"
                    : string.Join(", ", _client.RuleEngines.Modes);
                return Fail(
                    "No rule engine registered for mode \"" + modeKey + "\". Registered: " + known +
                    ". Call UseBizRuleEngines() or useRuleEngine(...).");
            }

            return engine.Execute(ruleSet, variables ?? new Dictionary<string, object>());
        }

        /// <summary>Validate <c>rule_content</c> for the given mode before save.</summary>
        public bool ValidateRuleContent(string mode, string ruleContent, out string message)
        {
            message = null;
            var modeKey = ResolveModeKey(mode);
            if (!_client.RuleEngines.TryGet(modeKey, out var engine))
            {
                message = "No rule engine registered for mode \"" + modeKey + "\"";
                return false;
            }

            return engine.ValidateRuleContent(ruleContent, out message);
        }

        /// <summary>
        /// 空 mode 默认 <see cref="RuleSetModeHelper.PersistedRuleTypes"/>；
        /// 其它字符串原样作为注册键（允许自定义引擎）。
        /// </summary>
        private static string ResolveModeKey(string persisted)
        {
            if (string.IsNullOrWhiteSpace(persisted))
                return RuleSetModeHelper.PersistedRuleTypes;
            return persisted.Trim();
        }

        private static RuleExecutionResultEntity Fail(string message) =>
            new RuleExecutionResultEntity
            {
                IsSuccess = false,
                Message = message,
                OutputVariables = new Dictionary<string, object>()
            };
    }
}
