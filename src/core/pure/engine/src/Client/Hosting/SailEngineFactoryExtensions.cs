using System;
using saiflow.engine.Client;
using saiflow.engine.Factories;

namespace saiflow.engine.Hosting;

/// <summary>
/// Builder：覆写四引擎工厂（与 UseFlowGate 同构，单一门面路径）。
/// 委托在 <see cref="SailClientBuilder.Build"/> 之后调用，以便注入本 Client。
/// </summary>
public static class SailEngineFactoryExtensions
{
    public static SailClientBuilder UseRuntimeFactory(
        this SailClientBuilder builder, Func<SailClient, IRuntimeFactory> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        builder.Services.Properties[SailBuilderKeys.RuntimeFactory] = factory;
        return builder;
    }

    public static SailClientBuilder UseMediatorFactory(
        this SailClientBuilder builder, Func<SailClient, IMediatorFactory> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        builder.Services.Properties[SailBuilderKeys.MediatorFactory] = factory;
        return builder;
    }

    public static SailClientBuilder UseProcessModelFactory(
        this SailClientBuilder builder, Func<SailClient, IProcessModelFactory> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        builder.Services.Properties[SailBuilderKeys.ProcessModelFactory] = factory;
        return builder;
    }

    public static SailClientBuilder UseManagerFactory(
        this SailClientBuilder builder, Func<SailClient, IManagerFactory> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        builder.Services.Properties[SailBuilderKeys.ManagerFactory] = factory;
        return builder;
    }

    /// <summary>Build 时挂载；未设则 Client 属性懒创建默认实现。</summary>
    internal static void ApplyPendingFactories(SailClient client, ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.RuntimeFactory, out var r)
            && r is Func<SailClient, IRuntimeFactory> runtime)
            client.RuntimeFactory = runtime(client);

        if (services.Properties.TryGetValue(SailBuilderKeys.MediatorFactory, out var m)
            && m is Func<SailClient, IMediatorFactory> mediator)
            client.MediatorFactory = mediator(client);

        if (services.Properties.TryGetValue(SailBuilderKeys.ProcessModelFactory, out var p)
            && p is Func<SailClient, IProcessModelFactory> processModel)
            client.ProcessModelFactory = processModel(client);

        if (services.Properties.TryGetValue(SailBuilderKeys.ManagerFactory, out var mg)
            && mg is Func<SailClient, IManagerFactory> managers)
            client.ManagerFactory = managers(client);
    }
}
