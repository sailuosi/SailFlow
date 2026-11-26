using System;
using sailflow.hosting;
using sailflow.storage;

namespace sailflow.runtime.storage;

/// <summary>IStorage 注册（仿 DI；唯一槽后覆盖）。</summary>
public static class SailStorageExtensions
{
    public const string FeatureName = "UseDefaultStorages";

    /// <summary>挂默认流程实例 Storage（可被后注册覆盖）。</summary>
    public static SailClient UseDefaultStorages(this SailClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        return client.useStorage<IProcessInstanceStorage>(new DefaultProcessInstanceStorage());
    }

    public static SailClient useStorage<TStorage>(this SailClient client, TStorage storage)
        where TStorage : class
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        client.Storages.Register(storage);
        return client;
    }

    public static SailClientBuilder UseDefaultStorages(this SailClientBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        builder.Services.UseDefaultStorages();
        return builder;
    }

    public static ISailCollection UseDefaultStorages(this ISailCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Phase = SailFeaturePhase.ConfigureClient,
            Order = 50,
            ConfigureClient = (client, _) => client.UseDefaultStorages()
        });
    }
}
