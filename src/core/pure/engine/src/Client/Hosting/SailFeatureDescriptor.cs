using System;

namespace saiflow.engine.Client.Hosting;

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
    public required string Name { get; init; }

    public SailFeaturePhase Phase { get; init; }

    public int Order { get; init; }

    /// <summary>ConfigureServices：仅操作 collection / MS DI。</summary>
    public Action<ISailCollection>? ConfigureServices { get; init; }

    /// <summary>ConfigureClient：骨架已 Assemble，可挂服务到 client。</summary>
    public Action<SailClient, ISailCollection>? ConfigureClient { get; init; }

    /// <summary>PostBuild：例如 EnsureTables / Seed。</summary>
    public Action<SailClient, ISailCollection>? PostBuild { get; init; }
}
