using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Hooks;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// Gate / Hook / Notifier 注册扩展（对齐 UseRuleEngine 管道）。
/// <see cref="UseFlowAspects"/> 为总包入口；<c>AddGate</c>/<c>AddHook</c>/<c>AddNotifier</c> 为无参构造追加别名。
/// </summary>
public static class SailFlowCapabilityExtensions
{
    /// <summary>收拢注册 Gate / Hook / Notifier（及可选 Outbox Drain）；<paramref name="configure"/> 参数即 services 自身。</summary>
    public static ISailCollection UseFlowAspects(
        this ISailCollection services,
        Action<ISailCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        return services;
    }

    /// <summary>收拢注册 Gate / Hook / Notifier（及可选 Outbox Drain）；<paramref name="configure"/> 参数即 builder 自身。</summary>
    public static SailClientBuilder UseFlowAspects(
        this SailClientBuilder builder,
        Action<SailClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder);
        return builder;
    }

    /// <summary>追加无参构造 Gate（等价 <c>UseFlowGate(new TGate(), configure)</c>）。</summary>
    public static ISailCollection AddGate<TGate>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where TGate : class, IFlowGate, new()
        => services.UseFlowGate(new TGate(), configure);

    /// <summary>追加无参构造 Hook（等价 <c>UseFlowHook(new THook(), configure)</c>）。</summary>
    public static ISailCollection AddHook<THook>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where THook : class, IFlowHook, new()
        => services.UseFlowHook(new THook(), configure);

    /// <summary>追加无参构造 Notifier（等价 <c>UseTaskNotifier(new TNotifier(), configure)</c>）。</summary>
    public static ISailCollection AddNotifier<TNotifier>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where TNotifier : class, ITaskNotifier, new()
        => services.UseTaskNotifier(new TNotifier(), configure);

    /// <summary>追加无参构造 Gate（等价 <c>UseFlowGate(new TGate(), configure)</c>）。</summary>
    public static SailClientBuilder AddGate<TGate>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where TGate : class, IFlowGate, new()
    {
        builder.Services.AddGate<TGate>(configure);
        return builder;
    }

    /// <summary>追加无参构造 Hook（等价 <c>UseFlowHook(new THook(), configure)</c>）。</summary>
    public static SailClientBuilder AddHook<THook>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where THook : class, IFlowHook, new()
    {
        builder.Services.AddHook<THook>(configure);
        return builder;
    }

    /// <summary>追加无参构造 Notifier（等价 <c>UseTaskNotifier(new TNotifier(), configure)</c>）。</summary>
    public static SailClientBuilder AddNotifier<TNotifier>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where TNotifier : class, ITaskNotifier, new()
    {
        builder.Services.AddNotifier<TNotifier>(configure);
        return builder;
    }

    public static ISailCollection UseFlowGate(
        this ISailCollection services,
        IFlowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(gate);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterGate(gate, options));
        services.ProvideFeature(SailFeatureNames.FlowGate, order: 30);
        return services;
    }

    public static ISailCollection UseFlowGate(
        this ISailCollection services,
        string name,
        Func<FlowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return services.UseFlowGate(new DelegateFlowGate(name, validate), configure);
    }

    public static ISailCollection UseFlowHook(
        this ISailCollection services,
        IFlowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hook);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterHook(hook, options));
        services.ProvideFeature(SailFeatureNames.FlowHook, order: 31);
        return services;
    }

    public static ISailCollection UseFlowHook(
        this ISailCollection services,
        string name,
        Action<FlowHookContext> handle,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return services.UseFlowHook(new DelegateFlowHook(name, handle), configure);
    }

    public static ISailCollection UseTaskNotifier(
        this ISailCollection services,
        ITaskNotifier notifier,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(notifier);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterNotifier(notifier, options));
        services.ProvideFeature(SailFeatureNames.TaskNotifier, order: 32);
        return services;
    }

    public static ISailCollection UseTaskNotifier(
        this ISailCollection services,
        string name,
        Func<TaskNotifyContext, CancellationToken, Task> notify,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(notify);
        return services.UseTaskNotifier(new DelegateTaskNotifier(name, notify), configure);
    }

    public static SailClientBuilder UseFlowGate(
        this SailClientBuilder builder,
        IFlowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowGate(gate, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowGate(
        this SailClientBuilder builder,
        string name,
        Func<FlowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowGate(name, validate, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowHook(
        this SailClientBuilder builder,
        IFlowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowHook(hook, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowHook(
        this SailClientBuilder builder,
        string name,
        Action<FlowHookContext> handle,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowHook(name, handle, configure);
        return builder;
    }

    public static SailClientBuilder UseTaskNotifier(
        this SailClientBuilder builder,
        ITaskNotifier notifier,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseTaskNotifier(notifier, configure);
        return builder;
    }

    public static SailClientBuilder UseTaskNotifier(
        this SailClientBuilder builder,
        string name,
        Func<TaskNotifyContext, CancellationToken, Task> notify,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseTaskNotifier(name, notify, configure);
        return builder;
    }

    public static SailClient useFlowGate(this SailClient client, IFlowGate gate, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterGate(gate, ApplyOptions(configure));
        return client;
    }

    public static SailClient useFlowHook(this SailClient client, IFlowHook hook, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterHook(hook, ApplyOptions(configure));
        return client;
    }

    public static SailClient useTaskNotifier(this SailClient client, ITaskNotifier notifier, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterNotifier(notifier, ApplyOptions(configure));
        return client;
    }

    internal static List<Action<FlowCapabilityRegistry>> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.FlowCapabilities, out var box)
            && box is List<Action<FlowCapabilityRegistry>> list)
            return list;

        list = new List<Action<FlowCapabilityRegistry>>();
        services.Properties[SailBuilderKeys.FlowCapabilities] = list;
        return list;
    }

    internal static void ApplyPendingRegistrations(SailClient client, ISailCollection services)
    {
        if (!services.Properties.TryGetValue(SailBuilderKeys.FlowCapabilities, out var box)
            || box is not List<Action<FlowCapabilityRegistry>> pending)
            return;

        foreach (var action in pending)
            action(client.Capabilities);
    }

    private static HookRegistrationOptions ApplyOptions(Action<HookRegistrationOptions> configure)
    {
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        return options;
    }
}
