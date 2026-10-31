using System;
using System.Threading;
using saiflow.engine.Hooks;

namespace saiflow.engine.Client.Hosting;

/// <summary>Outbox drain 宿主扩展（配置落 <see cref="SailClient.HookOutboxOptions"/>；<c>autoStart</c> 为 opt-in）。</summary>
public static class SailHookOutboxDrainExtensions
{
    public static ISailCollection UseHookOutboxDrain(
        this ISailCollection services,
        Action<HookOutboxOptions> configure = null,
        bool autoStart = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        var registration = new HookOutboxDrainRegistration { AutoStart = autoStart };
        configure?.Invoke(registration.Options);
        services.Properties[SailBuilderKeys.HookOutboxDrain] = registration;

        services.AddFeature(new SailFeatureDescriptor
        {
            Name = "HookOutboxDrain",
            Phase = SailFeaturePhase.ConfigureClient,
            Order = 160,
            ConfigureClient = ApplyToClient,
        });

        if (autoStart)
        {
            services.AddFeature(new SailFeatureDescriptor
            {
                Name = "HookOutboxDrain.AutoStart",
                Phase = SailFeaturePhase.PostBuild,
                Order = 960,
                PostBuild = StartDrainIfRequested,
            });
        }

        return services;
    }

    public static SailClientBuilder UseHookOutboxDrain(
        this SailClientBuilder builder,
        Action<HookOutboxOptions> configure = null,
        bool autoStart = false)
    {
        builder.Services.UseHookOutboxDrain(configure, autoStart);
        return builder;
    }

    internal static void ApplyToClient(SailClient client, ISailCollection services)
    {
        if (!services.Properties.TryGetValue(SailBuilderKeys.HookOutboxDrain, out var box)
            || box is not HookOutboxDrainRegistration registration)
            return;

        client.HookOutboxOptions ??= new HookOutboxOptions();
        CopyOptions(registration.Options, client.HookOutboxOptions);
    }

    private static void StartDrainIfRequested(SailClient client, ISailCollection services)
    {
        if (!services.Properties.TryGetValue(SailBuilderKeys.HookOutboxDrain, out var box)
            || box is not HookOutboxDrainRegistration registration
            || !registration.AutoStart)
            return;

        client.HookOutboxDrainCts?.Cancel();
        client.HookOutboxDrainCts?.Dispose();
        client.HookOutboxDrainCts = HookOutboxDrainLoop.StartBackground(client, client.HookOutboxOptions);
    }

    private static void CopyOptions(HookOutboxOptions source, HookOutboxOptions target)
    {
        target.MaxAttempts = source.MaxAttempts;
        target.BatchSize = source.BatchSize;
        target.ProcessingStaleSeconds = source.ProcessingStaleSeconds;
        target.LeaseSeconds = source.LeaseSeconds;
        target.PollInterval = source.PollInterval;
    }

    private sealed class HookOutboxDrainRegistration
    {
        public HookOutboxOptions Options { get; } = new();
        public bool AutoStart { get; set; }
    }
}
