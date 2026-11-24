using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.biz.Approval;
using sailflow.engine.Client;
using sailflow.events;
using sailflow.engine.Hosting;
using sailflow.hooks;

namespace sailflow.biz.Hosting;

/// <summary>审批业务门面：DI 登记 + Sync 终态 Hook + Completing Gate。</summary>
public static class ApprovalBusinessSailExtensions
{
    public const string FeatureName = "sail.approval.lifecycle";
    const string PendingKey = "sail.approval.pendingHandlers";
    const string UsedKey = "sail.approval.lifecycle.used";

    /// <summary>登记无参构造 Handler（Build 时实例化；同 FormCode 重复失败）。</summary>
    public static SailClientBuilder AddApprovalBusiness<T>(this SailClientBuilder builder)
        where T : class, ISailApprovalBusiness, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddApprovalBusiness(_ => new T());
    }

    /// <summary>登记工厂（可依赖 <see cref="SailClient"/>）。</summary>
    public static SailClientBuilder AddApprovalBusiness(
        this SailClientBuilder builder,
        Func<SailClient, ISailApprovalBusiness> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        GetPending(builder.Services).Add(factory);
        return builder;
    }

    /// <summary>
    /// PostBuild：挂 Sync Lifecycle Hook（Finish/Aborted）与 Completing Gate。
    /// 重复调用失败；勿再并行挂旧三事件全进 OnFinish 的 Hook。
    /// </summary>
    public static SailClientBuilder UseApprovalBusiness(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Properties.TryGetValue(UsedKey, out _))
        {
            throw new InvalidOperationException(
                "UseApprovalBusiness already registered. Do not attach a second approval lifecycle hook.");
        }
        builder.Services.Properties[UsedKey] = true;

        builder.Services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Phase = SailFeaturePhase.PostBuild,
            Order = 190,
            PostBuild = (client, services) => Register(client, services)
        });
        return builder;
    }

    /// <summary>已 Build 的 Client 上挂载（测试/补挂）。</summary>
    public static SailClient useApprovalBusiness(
        this SailClient client,
        IEnumerable<ISailApprovalBusiness> handlers,
        bool registerCompletingGate = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        var registry = new SailApprovalBusinessRegistry(handlers);
        Attach(client, registry, registerCompletingGate);
        return client;
    }

    static void Register(SailClient client, ISailCollection services)
    {
        var factories = GetPending(services);
        var list = new List<ISailApprovalBusiness>(factories.Count);
        foreach (var f in factories)
            list.Add(f(client));

        var registry = new SailApprovalBusinessRegistry(list);
        Attach(client, registry, registerCompletingGate: true);
    }

    static void Attach(
        SailClient client,
        SailApprovalBusinessRegistry registry,
        bool registerCompletingGate)
    {
        if (client.Capabilities.Hooks.Any(h =>
                string.Equals(h.Name, SailApprovalBusinessLifecycleHook.HookName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Hook '{SailApprovalBusinessLifecycleHook.HookName}' already registered.");
        }

        client.addHook(new SailApprovalBusinessLifecycleHook(registry), o =>
        {
            o.Match = HookMatch.All;
            o.Mode = HookMode.Sync;
            o.Events =
            [
                EventFireTypeEnum.OnProcessCompleted,
                EventFireTypeEnum.OnProcessRejected,
                EventFireTypeEnum.OnProcessClosed,
                EventFireTypeEnum.OnProcessCanceled,
                EventFireTypeEnum.OnProcessDiscarded,
                EventFireTypeEnum.OnProcessTerminated,
                EventFireTypeEnum.OnProcessWithdrawn
            ];
        });

        if (!registerCompletingGate)
            return;

        if (client.Capabilities.Gates.Any(g =>
                string.Equals(g.Name, SailApprovalCompletingGate.GateName, StringComparison.OrdinalIgnoreCase)))
            return;

        client.addGate(new SailApprovalCompletingGate(registry), o =>
        {
            o.Match = HookMatch.All;
        });
    }

    static List<Func<SailClient, ISailApprovalBusiness>> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(PendingKey, out var box)
            && box is List<Func<SailClient, ISailApprovalBusiness>> list)
            return list;

        list = new List<Func<SailClient, ISailApprovalBusiness>>();
        services.Properties[PendingKey] = list;
        return list;
    }
}
