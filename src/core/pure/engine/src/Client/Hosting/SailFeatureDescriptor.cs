using System;
using System.Collections.Generic;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

public enum SailFeaturePhase
{
    /// <summary>注册服务/Locator（Build 前写入集合）。</summary>
    ConfigureServices = 0,

    /// <summary>在 SailClient 骨架就绪后执行（可访问 client.UseRunSQL）。</summary>
    ConfigureClient = 1,

    /// <summary>Build 末尾（建表、种子、热身）。</summary>
    PostBuild = 2
}

/// <summary>
/// 一次 UseXxx 贡献的注册项。可多次调用同一 phase。
/// </summary>
public sealed class SailFeatureDescriptor
{
    /// <summary>Feature name used for idempotent replace (same Name + Phase).</summary>
    public string Name { get; init; } = string.Empty;

    public SailFeaturePhase Phase { get; init; }

    public int Order { get; init; }

    /// <summary>
    /// 依赖的其它能力 <see cref="Name"/>（跨 Phase）。Build 时由
    /// <see cref="SailFeatureDependencyValidator"/> 校验；缺依赖则失败。
    /// </summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary>ConfigureServices：仅操作 collection / MS DI。</summary>
    public Action<ISailCollection>? ConfigureServices { get; init; }

    /// <summary>ConfigureClient：骨架已 Assemble，可挂服务到 client。</summary>
    public Action<SailClient, ISailCollection>? ConfigureClient { get; init; }

    /// <summary>PostBuild：例如 EnsureTables / Seed。</summary>
    public Action<SailClient, ISailCollection>? PostBuild { get; init; }
}
