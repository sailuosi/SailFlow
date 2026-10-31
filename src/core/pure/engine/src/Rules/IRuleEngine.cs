using System.Collections.Generic;
using saiflow.engine.Business.Entity;

namespace saiflow.engine.Rules
{
    /// <summary>
    /// 规则引擎契约：按 <see cref="Mode"/>（对应 <c>wf_rule_set.mode</c>）执行与校验。
    /// 具体实现由业务侧注册（内置 NRules / bindingsJson，可继续扩展）。
    /// </summary>
    public interface IRuleEngine
    {
        /// <summary>
        /// 模式键，与持久化 <c>wf_rule_set.mode</c> 一致（如 <c>ruleTypes</c>、<c>bindingsJson</c>）。
        /// </summary>
        string Mode { get; }

        /// <summary>
        /// 执行规则集：输入变量 → 输出变量。
        /// </summary>
        RuleExecutionResultEntity Execute(RuleSetEntity ruleSet, IDictionary<string, object> variables);

        /// <summary>
        /// 校验 <c>rule_content</c> 是否符合本引擎约定（保存前可选调用）。
        /// </summary>
        bool ValidateRuleContent(string ruleContent, out string message);
    }
}
