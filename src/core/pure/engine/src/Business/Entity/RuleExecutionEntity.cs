using System.Collections.Generic;

namespace sailflow.engine.Entity
{
    public class RuleExecutionEntity
    {
        public string RuleSetCode { get; set; }
        public IDictionary<string, object> Variables { get; set; }
    }

    public class RuleExecutionResultEntity
    {
        public bool? IsSuccess { get; set; }
        public string Message { get; set; }
        public IDictionary<string, object> OutputVariables { get; set; }
    }

    /// <summary>
    /// 规则输入事实（NRules Fluent 等引擎可复用）。
    /// </summary>
    public class RuleInputFact
    {
        public IDictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 规则输出事实（NRules Fluent 等引擎可复用）。
    /// </summary>
    public class RuleOutputFact
    {
        public IDictionary<string, object> Vars { get; set; } = new Dictionary<string, object>();

        public void Set(string name, object value)
        {
            Vars[name] = value;
        }
    }
}

