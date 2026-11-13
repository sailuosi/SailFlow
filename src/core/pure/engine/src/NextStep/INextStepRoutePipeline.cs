using System.Collections.Generic;

namespace sailflow.engine.NextStep;

/// <summary>下一跳节点计算管道：Prepare → Evaluator → Filter。</summary>
public interface INextStepRoutePipeline
{
    void AddPrepare(INextStepPrepare prepare, NextStepRouteOptions options);

    void AddFilter(INextStepFilter filter, NextStepRouteOptions options);

    void AddConditionEvaluator(IConditionEvaluator evaluator, NextStepRouteOptions options);

    /// <summary>R0：按序应用 Prepare（写 Conditions）。</summary>
    void Prepare(NextStepRouteContext ctx);

    /// <summary>
    /// 求值条件表达式。无注册 Evaluator 时回落 LINQ Dynamic（≡ ExpressionParser）。
    /// 全链 null → false。
    /// </summary>
    bool Evaluate(ConditionEvaluateContext ctx);

    /// <summary>R1：按序 Filter 候选（S2 接线；S0 可调用为空操作若无 Filter）。</summary>
    void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates);

    IReadOnlyList<(IConditionEvaluator Evaluator, NextStepRouteOptions Options)> Evaluators { get; }
}
