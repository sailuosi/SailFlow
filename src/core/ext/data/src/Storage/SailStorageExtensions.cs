using System;
using sailflow.hosting;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>IStorage 注册（仿 DI；唯一槽后覆盖）。</summary>
public static class SailStorageExtensions
{
    public const string FeatureName = "UseDefaultStorages";

    /// <summary>
    /// 挂默认十二件套：运行时六 + 定义四 + Log/Archive（可被后注册覆盖）。
    /// </summary>
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
        client.useStorage<IProcessDefinitionStorage>(new DefaultProcessDefinitionStorage());
        client.useStorage<IVariableDefinitionStorage>(new DefaultVariableDefinitionStorage());
        client.useStorage<IRuleSetStorage>(new DefaultRuleSetStorage());
        client.useStorage<IJobInfoStorage>(new DefaultJobInfoStorage());
        client.useStorage<IEngineLogStorage>(new DefaultEngineLogStorage());
        client.useStorage<IProcessArchiveStorage>(new DefaultProcessArchiveStorage());
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
