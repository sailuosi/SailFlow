using System;
using System.Linq;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.engine.NextStep;
using sailflow.runtime.NextStep;

namespace sailflow.runtime.Hosting;

/// <summary>
/// 引擎默认扩展能力总包（pure、无重 NuGet）。
/// 当前：NextStep 默认 LINQ Evaluator。Assignee 仍见 <c>UseAssign</c> / <c>sailflow.assignee</c>（后续迁入本包）。
/// </summary>
public static class SailRuntimeExtensions
{
    public const string FeatureName = SailFeatureNames.SailRuntimeDefaults;

    /// <summary>
    /// 挂载 pure 默认实现：为 NextStep 管道注册 <see cref="LinqDynamicConditionEvaluator"/>（Order=100, Match=All）。
    /// 可与宿主自己的 <c>UseNextStepRoute</c> 并存（同 pending 表按序 Apply）。
    /// </summary>
    public static SailClientBuilder UseSailRuntimeDefaults(this SailClientBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        builder.Services.UseSailRuntimeDefaults();
        return builder;
    }

    public static ISailCollection UseSailRuntimeDefaults(this ISailCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // 复用 NextStep 单一注册表；不另开 Evaluator 表
        services.UseNextStepRoute(r =>
        {
            r.WithoutDefaultLinqEvaluator();
            r.AddConditionEvaluator(new LinqDynamicConditionEvaluator(), o =>
            {
                o.Order = 100;
                o.Match = NextStepMatch.All;
            });
        });

        // 占位 Feature：便于 ProbeInstalledFeatures / DependsOn
        if (!services.Any(f => string.Equals(f.Name, FeatureName, StringComparison.Ordinal)))
        {
            services.ProvideFeature(FeatureName, order: 44);
        }

        return services;
    }

    /// <summary>已 Build 的 Client：补挂默认 LINQ Evaluator（若管道尚无 Evaluator）。</summary>
    public static SailClient UseSailRuntimeDefaults(this SailClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        client.UseNextStepRoute(r =>
        {
            r.WithoutDefaultLinqEvaluator();
            r.AddConditionEvaluator(new LinqDynamicConditionEvaluator(), o =>
            {
                o.Order = 100;
                o.Match = NextStepMatch.All;
            });
        });
        return client;
    }
}
