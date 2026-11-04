using System;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

/// <summary>
/// 冷热归档 / 日志分表等生命周期 Options 注册（类比 <see cref="SailConfigExtensions"/>）。
/// 不解析配置文件；宿主可 <c>configuration.GetSection("Sail:DataLifecycle").Bind(opts)</c> 后传入。
/// </summary>
public static class SailDataLifecycleExtensions
{
    /// <summary>登记完整 Options 实例；Build 时 Apply 到 <see cref="SailClient.DataLifecycleOptions"/>。</summary>
    public static ISailCollection UseDataLifecycle(this ISailCollection services, SailDataLifecycleOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        services.Properties[SailBuilderKeys.DataLifecycle] = options;
        return services;
    }

    /// <summary>登记对默认 Options 的修改委托。</summary>
    public static ISailCollection UseDataLifecycle(this ISailCollection services, Action<SailDataLifecycleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Properties[SailBuilderKeys.DataLifecycleAction] = configure;
        return services;
    }

    /// <summary>Builder：写入完整 Options。</summary>
    public static SailClientBuilder UseDataLifecycle(this SailClientBuilder builder, SailDataLifecycleOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        builder.Services.UseDataLifecycle(options);
        return builder;
    }

    /// <summary>Builder：就地修改默认值（保留窗、冷库、日志分表等）。</summary>
    public static SailClientBuilder UseDataLifecycle(this SailClientBuilder builder, Action<SailDataLifecycleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.UseDataLifecycle(configure);
        return builder;
    }

    internal static void ApplyToClient(SailClient client, ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.DataLifecycle, out var box)
            && box is SailDataLifecycleOptions options)
        {
            client.DataLifecycleOptions.Apply(options);
        }

        if (services.Properties.TryGetValue(SailBuilderKeys.DataLifecycleAction, out var actionBox)
            && actionBox is Action<SailDataLifecycleOptions> configure)
        {
            configure(client.DataLifecycleOptions);
        }
    }
}
