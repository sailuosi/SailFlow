using System;
using sailflow.engine.NextStep;
using sailflow.engine.Xpdl;

namespace sailflow.runtime.NextStep;

/// <summary>
/// 默认条件求值：对齐 <see cref="ExpressionParser"/>（LINQ Dynamic）。
/// 经 <c>UseSailRuntimeDefaults</c> 挂入 <c>UseNextStepRoute</c>；不随 engine 自动注册。
/// 未配置 Evaluator 时引擎条件求值直接抛异常（无 ExpressionParser 静默回落）。
/// </summary>
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
            return null;
        }
    }
}
