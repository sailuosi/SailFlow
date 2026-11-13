using System;
using System.Collections.Generic;
using sailflow.engine.Actions;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// Action / Script / Service 方法处理器注册。
/// </summary>
public static class SailActionMethodExtensions
{
    /// <summary>追加 Action 方法处理器（可多次；同 Method 后注册覆盖）。</summary>
    public static ISailCollection AddActionMethod(this ISailCollection services, IActionMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);
        GetOrCreateActionList(services).Add(handler);
        return services;
    }

    /// <summary>追加 Script 方法处理器（可多次）。</summary>
    public static ISailCollection AddScriptMethod(this ISailCollection services, IScriptMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);
        GetOrCreateScriptList(services).Add(handler);
        return services;
    }

    /// <summary>追加 Service 方法处理器（可多次）。</summary>
    public static ISailCollection AddServiceMethod(this ISailCollection services, IServiceMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);
        GetOrCreateServiceList(services).Add(handler);
        return services;
    }

    internal static List<IActionMethodHandler> GetOrCreateActionList(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.ActionMethods, out var box)
            && box is List<IActionMethodHandler> list)
            return list;
        list = new List<IActionMethodHandler>();
        services.Properties[SailBuilderKeys.ActionMethods] = list;
        return list;
    }

    internal static List<IScriptMethodHandler> GetOrCreateScriptList(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.ScriptMethods, out var box)
            && box is List<IScriptMethodHandler> list)
            return list;
        list = new List<IScriptMethodHandler>();
        services.Properties[SailBuilderKeys.ScriptMethods] = list;
        return list;
    }

    internal static List<IServiceMethodHandler> GetOrCreateServiceList(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.ServiceMethods, out var box)
            && box is List<IServiceMethodHandler> list)
            return list;
        list = new List<IServiceMethodHandler>();
        services.Properties[SailBuilderKeys.ServiceMethods] = list;
        return list;
    }
}
