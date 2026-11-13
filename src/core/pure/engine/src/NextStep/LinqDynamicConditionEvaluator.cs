using System;
using sailflow.engine.Xpdl;

namespace sailflow.engine.NextStep;

/// <summary>默认条件求值：对齐今日 <see cref="ExpressionParser"/>。</summary>
public sealed class LinqDynamicConditionEvaluator : IConditionEvaluator
{
    public bool IsMatch(ConditionEvaluateContext ctx) => true;

    public bool? TryEvaluate(ConditionEvaluateContext ctx)
    {
        if (ctx == null || string.IsNullOrWhiteSpace(ctx.Expression))
            return true;

        try
        {
            var vars = ctx.Variables;
            var expression = ctx.Expression;
            if (vars != null && vars.Count > 0)
                expression = ExpressionParser.ReplaceParameterToValue(expression, vars);
            return ExpressionParser.Parse(expression);
        }
        catch (Exception)
        {
            // 与历史 IsValidTransition 吞异常后当 false 不同：
            // 作为链中一环返回 null，便于后续 Evaluator；若为唯一兜底由管道将全 null 收成 false。
            return null;
        }
    }

    /// <summary>单次求值（无管道时的回落，失败 → false，兼容历史）。</summary>
    public static bool EvaluateLegacy(string expression, System.Collections.Generic.IDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true;
        try
        {
            var text = expression;
            if (variables != null && variables.Count > 0)
                text = ExpressionParser.ReplaceParameterToValue(text, variables);
            return ExpressionParser.Parse(text);
        }
        catch
        {
            return false;
        }
    }
}
