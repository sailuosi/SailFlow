using System;
using saiflow.engine.Client;
using saiflow.engine.Runtime;

namespace saiflow.engine.Hosting;

/// <summary>
/// 可选：定时清理 <see cref="InstanceExecutionLock"/> 空闲条目（Timer，默认不启用）。
/// </summary>
public static class SailInstanceLockCleanerExtensions
{
    public static ISailCollection UseInstanceLockCleaner(
        this ISailCollection services,
        Action<InstanceLockCleanerOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new InstanceLockCleanerOptions();
        configure?.Invoke(options);
        services.Properties[SailBuilderKeys.InstanceLockCleaner] = options;

        services.AddFeature(new SailFeatureDescriptor
        {
            Name = SailFeatureNames.InstanceLockCleaner,
            Phase = SailFeaturePhase.PostBuild,
            Order = 970,
            PostBuild = (_, svc) =>
            {
                if (!svc.Properties.TryGetValue(SailBuilderKeys.InstanceLockCleaner, out var box)
                    || box is not InstanceLockCleanerOptions opts)
                    return;
                InstanceLockCleaner.Start(opts);
            },
        });

        return services;
    }

    public static SailClientBuilder UseInstanceLockCleaner(
        this SailClientBuilder builder,
        Action<InstanceLockCleanerOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.UseInstanceLockCleaner(configure);
        return builder;
    }

    /// <summary>已有 Client 时即时启用（测试/热补挂）。</summary>
    public static SailClient useInstanceLockCleaner(
        this SailClient client,
        Action<InstanceLockCleanerOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var options = new InstanceLockCleanerOptions();
        configure?.Invoke(options);
        InstanceLockCleaner.Start(options);
        return client;
    }
}
