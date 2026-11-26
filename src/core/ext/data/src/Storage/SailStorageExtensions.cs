using System;
using sailflow.hosting;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>IStorage 注册（仿 DI；唯一槽后覆盖）。</summary>
public static class SailStorageExtensions
{
    public const string FeatureName = "UseDefaultStorages";

    /// <summary>挂默认 PI / Task / AI / Transition / HookOutbox / Variable Storage（可被后注册覆盖）。</summary>
    public static SailClient UseDefaultStorages(this SailClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        client.useStorage<IProcessInstanceStorage>(new DefaultProcessInstanceStorage());
        client.useStorage<ITaskStorage>(new DefaultTaskStorage());
        client.useStorage<IActivityInstanceStorage>(new DefaultActivityInstanceStorage());
        client.useStorage<ITransitionInstanceStorage>(new DefaultTransitionInstanceStorage());
        client.useStorage<IHookOutboxStorage>(new DefaultHookOutboxStorage());
        client.useStorage<IProcessVariableStorage>(new DefaultProcessVariableStorage());
        return client;
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
