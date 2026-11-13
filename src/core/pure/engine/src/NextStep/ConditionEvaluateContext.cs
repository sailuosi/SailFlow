using System.Collections.Generic;
using sailflow.engine.Client;

namespace sailflow.engine.NextStep;

/// <summary>条件表达式求值上下文。</summary>
public sealed class ConditionEvaluateContext
{
    public SailClient Sail { get; set; }

    /// <summary>conditionExpression / Trigger 表达式原文。</summary>
    public string Expression { get; set; }

    /// <summary>已合并的条件袋。</summary>
    public IDictionary<string, string> Variables { get; set; }

    public string ProcessId { get; set; }
    public string ProcessCode { get; set; }
    public string CurrentActivityId { get; set; }
    public string CurrentActivityCode { get; set; }
    public string TransitionId { get; set; }
    public NextStepRoutePhase Phase { get; set; }
}
