using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.engine.Actions;
using sailflow.engine.Assignee;
using sailflow.engine.NextStep;
using sailflow.engine.Rules;
using sailflow.hub.Essential;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// Fluent builder for <see cref="SailClient"/> (类比 WebApplicationBuilder)。
/// </summary>
public sealed class SailClientBuilder
{
    public ISailCollection Services { get; } = new SailCollection();

    /// <summary>尚未 Build 的数据库工厂；也可由 UseDatabase 写入 Properties。</summary>
    public SailDBFactory? DbFactory { get; set; }

    /// <summary>可选：Build 前指定的缓存；未设则默认 <see cref="MemorySooCache"/>。</summary>
    public ISooCache? Cache { get; set; }

    /// <summary>可选：Build 前登记的配置形状（或委托）。</summary>
    public SailConfig? Config { get; set; }

    /// <summary>可选：对默认 Config 的就地修改。</summary>
    public Action<SailConfig>? ConfigAction { get; set; }

    public SailClientBuilder UseDatabase(SailDBFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        DbFactory = factory;
        Services.UseDatabase(factory);
        return this;
    }

    public SailClientBuilder UseDatabase(mooSQL.data.DBInstance db, int mainPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(db);
        return UseDatabase(new DelegateSailDBFactory(db, mainPosition));
    }

    /// <summary>
    /// 注册缓存（对齐 mooSQL <c>builder.useCache(...)</c> / <c>client.useCache(...)</c>）。
    /// </summary>
    public SailClientBuilder UseCache(ISooCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        Cache = cache;
        Services.UseCache(cache);
        return this;
    }

    /// <summary>
    /// 注册运行参数形状（类比 mooSQL <c>useDataBase(List&lt;DBPosition&gt;)</c>）。
    /// 不解析 IConfiguration / AppSettings；业务侧可先装载再传入。
    /// </summary>
    public SailClientBuilder UseConfig(SailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Config = config;
        Services.UseConfig(config);
        return this;
    }

    /// <summary>登记对默认配置形状的修改。</summary>
    public SailClientBuilder UseConfig(Action<SailConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ConfigAction = configure;
        Services.UseConfig(configure);
        return this;
    }

    /// <summary>追加规则引擎（按 mode 扩展；可多次调用）。</summary>
    public SailClientBuilder AddRuleEngine(IRuleEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        Services.AddRuleEngine(engine);
        return this;
    }

    /// <summary>追加 Action 方法处理器（可多次；同 Method 后注册覆盖）。</summary>
    public SailClientBuilder AddActionMethod(IActionMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Services.AddActionMethod(handler);
        return this;
    }

    /// <summary>追加 Script 方法处理器（可多次）。</summary>
    public SailClientBuilder AddScriptMethod(IScriptMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Services.AddScriptMethod(handler);
        return this;
    }

    /// <summary>追加 Service 方法处理器（可多次）。</summary>
    public SailClientBuilder AddServiceMethod(IServiceMethodHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Services.AddServiceMethod(handler);
        return this;
    }

    /// <summary>注册消息队列实现（Message/Signal Throw 发布）。</summary>
    public SailClientBuilder UseMessageQueue(IMessageQueueService messageQueue)
    {
        ArgumentNullException.ThrowIfNull(messageQueue);
        Services.UseMessageQueue(messageQueue);
        return this;
    }

    /// <summary>注册 JSON 序列化实现（下一步执行人 / MQ 载荷 / Hook Outbox / 日志等）。</summary>
    public SailClientBuilder UseJsonSerializer(IJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        Services.UseJsonSerializer(serializer);
        return this;
    }

    public SailClientBuilder ConfigureServices(Action<ISailCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <summary>积极构建：得到 <see cref="SailClient"/>，由宿主自行持有并注入工作类。</summary>
    public SailClient Build()
    {
        SailFeatureDependencyValidator.Validate(Services);

        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.ConfigureServices))
        {
            d.ConfigureServices?.Invoke(Services);
        }

        var dbFactory = DbFactory;
        if (dbFactory == null
            && Services.Properties.TryGetValue(SailBuilderKeys.DbFactory, out var box))
        {
            dbFactory = box as SailDBFactory;
        }

        if (dbFactory == null)
        {
            throw new InvalidOperationException(
                "SailDBFactory is required. Call builder.UseDatabase(...) or Services.UseDatabase(...) before Build().");
        }

        var client = SailClientAssembler.Assemble(dbFactory);

        var config = Config;
        if (config == null
            && Services.Properties.TryGetValue(SailBuilderKeys.Config, out var configBox))
        {
            config = configBox as SailConfig;
        }

        if (config != null)
            client.useConfig(config);

        var configAction = ConfigAction;
        if (configAction == null
            && Services.Properties.TryGetValue(SailBuilderKeys.ConfigAction, out var actionBox))
        {
            configAction = actionBox as Action<SailConfig>;
        }

        if (configAction != null)
            client.useConfig(configAction);

        SailDataLifecycleExtensions.ApplyToClient(client, Services);

        var cache = Cache;
        if (cache == null
            && Services.Properties.TryGetValue(SailBuilderKeys.Cache, out var cacheBox))
        {
            cache = cacheBox as ISooCache;
        }

        if (cache != null)
            client.useCache(cache);

        if (Services.Properties.TryGetValue(SailBuilderKeys.RuleEngines, out var enginesBox)
            && enginesBox is List<IRuleEngine> engines)
        {
            foreach (var engine in engines)
                client.addRuleEngine(engine);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.ActionMethods, out var actionsBox)
            && actionsBox is List<IActionMethodHandler> actions)
        {
            foreach (var handler in actions)
                client.addActionMethod(handler);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.ScriptMethods, out var scriptsBox)
            && scriptsBox is List<IScriptMethodHandler> scripts)
        {
            foreach (var handler in scripts)
                client.addScriptMethod(handler);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.ServiceMethods, out var servicesBox)
            && servicesBox is List<IServiceMethodHandler> serviceHandlers)
        {
            foreach (var handler in serviceHandlers)
                client.addServiceMethod(handler);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.MessageQueue, out var mqBox)
            && mqBox is IMessageQueueService messageQueue)
        {
            client.useMessageQueue(messageQueue);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.JsonSerializer, out var jsonBox)
            && jsonBox is IJsonSerializer jsonSerializer)
        {
            client.useJsonSerializer(jsonSerializer);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.AssigneePipeline, out var assigneeBox)
            && assigneeBox is IAssigneePipeline assigneePipeline)
        {
            client.useAssigneePipeline(assigneePipeline);
        }

        if (Services.Properties.TryGetValue(SailBuilderKeys.NextStepRoute, out var nextStepBox)
            && nextStepBox is INextStepRoutePipeline nextStepRoute)
        {
            client.useNextStepRoute(nextStepRoute);
        }

        SailFlowCapabilityExtensions.ApplyPendingRegistrations(client, Services);
        SailEngineFactoryExtensions.ApplyPendingFactories(client, Services);
        SailIdExtensions.ApplyPending(client, Services);

        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.ConfigureClient))
        {
            d.ConfigureClient?.Invoke(client, Services);
        }

        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.PostBuild))
        {
            d.PostBuild?.Invoke(client, Services);
        }

        client.InstalledFeatures = Services
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .Select(d => new SailInstalledFeature
            {
                Name = d.Name,
                Phase = d.Phase,
                Order = d.Order,
                DependsOn = d.DependsOn is { Count: > 0 }
                    ? d.DependsOn.ToArray()
                    : Array.Empty<string>(),
            })
            .ToList();

        return client;
    }
}
