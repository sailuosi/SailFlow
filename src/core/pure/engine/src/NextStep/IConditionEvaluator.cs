namespace sailflow.engine.NextStep;

/// <summary>
/// 条件表达式求值（连线 / Conditional 等）。
/// <see cref="TryEvaluate"/> 返回 null 表示交下一 Order；true/false 终止链。
/// </summary>
public interface IConditionEvaluator
{
    bool IsMatch(ConditionEvaluateContext ctx);

    bool? TryEvaluate(ConditionEvaluateContext ctx);
}
