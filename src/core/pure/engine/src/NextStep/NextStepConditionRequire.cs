using System;
using sailflow.engine.Client;

namespace sailflow.engine.NextStep;

/// <summary>条件求值前置：无管道 / 无 Evaluator 时快速失败，迫使业务侧显式配置。</summary>
public static class NextStepConditionRequire
{
    public const string ConfigureHint =
        "No IConditionEvaluator registered for condition evaluation. "
        + "Call UseSailRuntimeDefaults() / UseBizRuntimeDefaults() "
        + "(registers LinqDynamicConditionEvaluator), "
        + "or UseNextStepRoute(r => r.AddConditionEvaluator<T>(...)). "
        + "Silent ExpressionParser fallback has been removed.";

    public static INextStepRoutePipeline GetPipelineRequired(SailClient sail)
    {
        var pipeline = sail?.NextStepRoute;
        if (pipeline != null)
            return pipeline;
        throw new InvalidOperationException(
            "No INextStepRoutePipeline on SailClient. "
            + "Call UseNextStepRoute(...) and register at least one IConditionEvaluator "
            + "(e.g. UseSailRuntimeDefaults / UseBizRuntimeDefaults).");
    }

    public static void ThrowIfNoEvaluators(INextStepRoutePipeline pipeline)
    {
        if (pipeline?.Evaluators != null && pipeline.Evaluators.Count > 0)
            return;
        throw new InvalidOperationException(ConfigureHint);
    }
}
