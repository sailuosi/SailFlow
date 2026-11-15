using System.Collections.Generic;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 规则执行请求（非落库）。
    /// 按规则集编码 + 输入变量字典调用 RuleEngine。
    /// </summary>
    public class RuleExecutionEntity
    {
        /// <summary>要执行的规则集编码，对应 <see cref="RuleSetEntity.RuleSetCode"/>。</summary>
        public string RuleSetCode { get; set; }

        /// <summary>输入变量袋（名称 → 值），注入规则引擎事实。</summary>
        public IDictionary<string, object> Variables { get; set; }
    }

    /// <summary>
    /// 规则执行结果（非落库）。
    /// </summary>
    public class RuleExecutionResultEntity
    {
        /// <summary>是否执行成功（引擎未抛错且规则逻辑判定通过时为 true；具体语义由引擎实现）。</summary>
        public bool? IsSuccess { get; set; }

        /// <summary>结果消息或失败原因。</summary>
        public string Message { get; set; }

        /// <summary>输出变量袋（规则写回的变量）。</summary>
        public IDictionary<string, object> OutputVariables { get; set; }
    }

    /// <summary>
    /// 规则输入事实（NRules Fluent 等引擎可复用）。
    /// </summary>
    public class RuleInputFact
    {
        /// <summary>输入变量字典。</summary>
        public IDictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 规则输出事实（NRules Fluent 等引擎可复用）。
    /// </summary>
    public class RuleOutputFact
    {
        /// <summary>输出变量字典。</summary>
        public IDictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();

        /// <summary>写入/覆盖一个输出变量。</summary>
        public void Set(string name, object value)
        {
            Vars[name] = value;
        }
    }
}
