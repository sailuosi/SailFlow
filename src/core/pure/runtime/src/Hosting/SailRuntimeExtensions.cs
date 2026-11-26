using System;
using System.Linq;
using sailflow.hosting;
using sailflow.factory;
using sailflow.nextStep;
using sailflow.runtime.Actions;
using sailflow.runtime.NextStep;
using sailflow.runtime.storage;

namespace sailflow.runtime.Hosting;

/// <summary>
/// 引擎默认扩展能力总包（pure、无重 NuGet）。
/// 当前：DefaultManagerFactory；NextStep LINQ Evaluator；Guid 发号；内置 Action/Script/Service。
/// Assignee 仍见 <c>UseAssign</c> / <c>sailflow.assignee</c>（后续迁入本包）。
/// </summary>
public static class SailRuntimeExtensions
{
    public const string FeatureName = SailFeatureNames.SailRuntimeDefaults;

    /// <summary>
    /// 挂载 pure 默认实现：Manager 工厂 + NextStep LINQ Evaluator + GuidString Id 发号 + 内置 Action 方法。
    /// 可与宿主自己的 <c>UseManagerFactory</c> / <c>UseNextStepRoute</c> / <c>UseSailIds</c> / <c>AddActionMethod</c> 并存（后注册覆盖）。
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

        services.Properties[SailBuilderKeys.ManagerFactory] =
            new Func<SailClient, IManagerFactory>(c => new DefaultManagerFactory(c));

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

        // 内置 Action/Script/Service（原 engine EnsurePorts 自动挂载）
        BuiltinActionMethods.RegisterAll(services);

        // IStorage S0：默认流程实例 Storage（kit 首参；可后覆盖）
        services.UseDefaultStorages();

        // 占位 Feature：便于 ProbeInstalledFeatures / DependsOn
        if (!services.Any(f => string.Equals(f.Name, FeatureName, StringComparison.Ordinal)))
        {
            services.ProvideFeature(FeatureName, order: 44);
        }

        return services;
    }

    /// <summary>已 Build 的 Client：补挂默认 Manager 工厂 + LINQ Evaluator + Guid 发号 + 内置 Action 方法。</summary>
    public static SailClient UseSailRuntimeDefaults(this SailClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        client.ManagerFactory = new DefaultManagerFactory(client);
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
        BuiltinActionMethods.RegisterAll(client);
        client.UseDefaultStorages();
        return client;
    }
}
