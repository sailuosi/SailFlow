using System;
using System.Linq;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.engine.NextStep;
using sailflow.runtime.NextStep;

namespace sailflow.runtime.Hosting;

/// <summary>
/// 引擎默认扩展能力总包（pure、无重 NuGet）。
/// 当前：NextStep 默认 LINQ Evaluator；内置 Guid 发号（<see cref="SailIdRuntimeExtensions.UseGuidString"/>）。
/// Assignee 仍见 <c>UseAssign</c> / <c>sailflow.assignee</c>（后续迁入本包）。
/// </summary>
public static class SailRuntimeExtensions
{
    public const string FeatureName = SailFeatureNames.SailRuntimeDefaults;

    /// <summary>
    /// 挂载 pure 默认实现：NextStep LINQ Evaluator + GuidString Id 发号。
    /// 可与宿主自己的 <c>UseNextStepRoute</c> / <c>UseSailIds</c> 并存（后注册覆盖）。
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

        // 内置 Guid 发号注册（实现位于 runtime.Ids）
        services.UseSailIdDefaults();

        // 占位 Feature：便于 ProbeInstalledFeatures / DependsOn
        if (!services.Any(f => string.Equals(f.Name, FeatureName, StringComparison.Ordinal)))
        {
            services.ProvideFeature(FeatureName, order: 44);
        }

        return services;
    }

    /// <summary>已 Build 的 Client：补挂默认 LINQ Evaluator + Guid 发号。</summary>
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
        client.UseSailIdDefaults();
        return client;
    }
}
