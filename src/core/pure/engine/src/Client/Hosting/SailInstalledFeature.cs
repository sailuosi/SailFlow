using System;
using System.Collections.Generic;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

/// <summary>Build 完成后挂在 <see cref="SailClient"/> 上的已装能力快照。</summary>
public sealed class SailInstalledFeature
{
    public string Name { get; init; } = string.Empty;

    public SailFeaturePhase Phase { get; init; }

    public int Order { get; init; }

    public IReadOnlyList<string> DependsOn { get; init; } = Array.Empty<string>();
}
