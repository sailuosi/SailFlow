using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Hooks;

namespace saiflow.engine.Client.Hosting;

/// <summary>Gate / Hook / Notifier 注册扩展（对齐 UseRuleEngine 管道）。</summary>
public static class SailWorkflowCapabilityExtensions
{
    public static ISailCollection UseFlowGate(
        this ISailCollection services,
        IWorkflowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(gate);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterGate(gate, options));
        return services;
    }

    public static ISailCollection UseFlowGate(
        this ISailCollection services,
        string name,
        Func<WorkflowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return services.UseFlowGate(new DelegateWorkflowGate(name, validate), configure);
    }

    public static ISailCollection UseFlowHook(
        this ISailCollection services,
        IWorkflowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hook);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterHook(hook, options));
        return services;
    }

    public static ISailCollection UseFlowHook(
        this ISailCollection services,
        string name,
        Action<WorkflowHookContext> handle,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return services.UseFlowHook(new DelegateWorkflowHook(name, handle), configure);
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
        IWorkflowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowGate(gate, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowGate(
        this SailClientBuilder builder,
        string name,
        Func<WorkflowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowGate(name, validate, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowHook(
        this SailClientBuilder builder,
        IWorkflowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.UseFlowHook(hook, configure);
        return builder;
    }

    public static SailClientBuilder UseFlowHook(
        this SailClientBuilder builder,
        string name,
        Action<WorkflowHookContext> handle,
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

    public static SailClient useFlowGate(this SailClient client, IWorkflowGate gate, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterGate(gate, ApplyOptions(configure));
        return client;
    }

    public static SailClient useFlowHook(this SailClient client, IWorkflowHook hook, Action<HookRegistrationOptions> configure = null)
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

    internal static List<Action<WorkflowCapabilityRegistry>> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.WorkflowCapabilities, out var box)
            && box is List<Action<WorkflowCapabilityRegistry>> list)
            return list;

        list = new List<Action<WorkflowCapabilityRegistry>>();
        services.Properties[SailBuilderKeys.WorkflowCapabilities] = list;
        return list;
    }

    internal static void ApplyPendingRegistrations(SailClient client, ISailCollection services)
    {
        if (!services.Properties.TryGetValue(SailBuilderKeys.WorkflowCapabilities, out var box)
            || box is not List<Action<WorkflowCapabilityRegistry>> pending)
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
