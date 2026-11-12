using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// Build 前校验 <see cref="SailFeatureDescriptor.DependsOn"/>：依赖名须已作为某 Feature 的 <see cref="SailFeatureDescriptor.Name"/> 注册（跨 Phase）。
/// </summary>
public static class SailFeatureDependencyValidator
{
    public static void Validate(IEnumerable<SailFeatureDescriptor> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        var list = features as IList<SailFeatureDescriptor> ?? features.ToList();
        if (list.Count == 0)
            return;

        var provided = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in list)
        {
            if (!string.IsNullOrWhiteSpace(f.Name))
                provided.Add(f.Name);
        }

        var errors = new List<string>();
        foreach (var f in list)
        {
            var deps = f.DependsOn;
            if (deps == null || deps.Count == 0)
                continue;

            foreach (var dep in deps)
            {
                if (string.IsNullOrWhiteSpace(dep))
                    continue;
                if (provided.Contains(dep))
                    continue;

                errors.Add(
                    $"Feature '{f.Name}' (Phase={f.Phase}) depends on '{dep}', which is not registered. " +
                    "Call the corresponding UseXxx (or ProvideFeature) before Build().");
            }
        }

        if (errors.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Sail feature dependency check failed:");
        foreach (var e in errors)
            sb.AppendLine(" - " + e);
        throw new InvalidOperationException(sb.ToString().TrimEnd());
    }
}
