using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

/// <summary>声明「本集合已提供某能力名」的薄标记（无副作用，供 DependsOn / 探针使用）。</summary>
public static class SailFeatureProvideExtensions
{
    /// <summary>
    /// 幂等登记能力名。同 Name+Phase 覆盖；用于 Property 插槽类 UseXxx（如 Json / Database）。
    /// </summary>
    public static ISailCollection ProvideFeature(
        this ISailCollection services,
        string name,
        SailFeaturePhase phase = SailFeaturePhase.ConfigureServices,
        int order = 0,
        IEnumerable<string>? dependsOn = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Feature name is required.", nameof(name));

        string[]? deps = null;
        if (dependsOn != null)
        {
            deps = dependsOn
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (deps.Length == 0)
                deps = null;
        }

        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = name.Trim(),
            Phase = phase,
            Order = order,
            DependsOn = deps,
        });
    }
}
