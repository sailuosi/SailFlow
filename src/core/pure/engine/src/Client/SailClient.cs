using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using mooSQL.data;
using saiflow.engine.Actions;
using saiflow.engine.Assignee;
using saiflow.engine.Hosting;
using saiflow.engine.Hooks;
using saiflow.engine.Rules;
using saiflow.hub.Essential;
using saiflow.hub.Localize;

namespace saiflow.engine.Client;

/// <summary>
/// 流程运行根实例（对齐 BeeFlow Client 模式）。
/// 具体 <see cref="DBInstance"/> 仅在应用宿主构建时注入，业务层通过 DI 获取本实例。
/// 数据访问终态约定：无事务用 <see cref="UseRunSQL"/>；有事务由上游灌入同一 <see cref="SQLBuilder"/>（begin/commit）。
/// 业务不持有 DBInstance，不依赖 DBExecutor。切片完成须符合约定，禁止中间态。详见 doc/slick/08。
/// </summary>
public partial class SailClient
{
    public SailDBFactory DBFactory { get; set; } = null!;

    public SailDialect Dialect { get; set; } = null!;

    public SailConfig Config { get; set; } = null!;

    /// <summary>
    /// Localization messages (JSON via <c>UseLocalize()</c>; default passthrough returns keys).
    /// </summary>
    public ILocalizeMessages Messages { get; set; } = PassthroughLocalizeMessages.Instance;

    private ISooCache _cache;

    /// <summary>
    /// 进程缓存（对齐 mooSQL <c>MooClient.Cache</c> / <see cref="ISooCache"/>）。
    /// 未显式 <see cref="useCache"/> 时默认 <see cref="MemorySooCache"/>（System.Runtime.Caching）。
    /// </summary>
    public ISooCache Cache
    {
        get
        {
            if (_cache == null)
                _cache = CreateDefaultCache();
            return _cache;
        }
    }

    /// <summary>事件插槽便捷访问（与 <see cref="SailDialect.Event"/> 同源）。</summary>
    public ISailEventHub Event => Dialect?.Event
        ?? throw new InvalidOperationException("SailClient.Dialect.Event is not mounted.");

    /// <summary>
    /// 主库实例快捷访问（装配/诊断用）。业务 Manager 请优先 <see cref="UseRunSQL"/>，勿扩散持有 DBInstance。
    /// </summary>
    public DBInstance GetDB() => DBFactory.GetDB();

    /// <summary>运行库 SQLBuilder（每请求独立实例；无事务即取即用，有事务则 begin 后向下传递）。</summary>
    public SQLBuilder UseRunSQL() => DBFactory.useRunSQL();

    /// <summary>
    /// 注册缓存实例（对齐 mooSQL <c>MooClient.useCache</c>）。可换 Redis 等自定义 <see cref="ISooCache"/>。
    /// </summary>
    public SailClient useCache(ISooCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        return this;
    }

    /// <summary>
    /// 注册运行参数形状（对齐 mooSQL <c>useDataBase(List&lt;DBPosition&gt;)</c>：登记形状，不解析配置源）。
    /// </summary>
    public SailClient useConfig(SailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (Config == null)
            Config = new SailConfig(this);
        Config.Apply(config);
        return this;
    }

    /// <summary>就地修改已挂载的 <see cref="Config"/>。</summary>
    public SailClient useConfig(Action<SailConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (Config == null)
            Config = new SailConfig(this);
        Config.Configure(configure);
        return this;
    }

    /// <summary>登记 / 合并冷热归档与日志分表 Options（不解析配置文件）。</summary>
    public SailClient useDataLifecycle(SailDataLifecycleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        DataLifecycleOptions ??= new SailDataLifecycleOptions();
        DataLifecycleOptions.Apply(options);
        return this;
    }

    /// <summary>就地修改 <see cref="DataLifecycleOptions"/>。</summary>
    public SailClient useDataLifecycle(Action<SailDataLifecycleOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataLifecycleOptions ??= new SailDataLifecycleOptions();
        configure(DataLifecycleOptions);
        return this;
    }

    private FlowCapabilityRegistry _capabilities;
    private FlowCapabilityDispatcher _capabilityDispatcher;

    private RuleEngineRegistry _ruleEngines;
    private ActionMethodRegistry _actionMethods;
    private ScriptMethodRegistry _scriptMethods;
    private ServiceMethodRegistry _serviceMethods;

    /// <summary>Gate / Hook / Notifier 注册表（Client 级）。</summary>
    public FlowCapabilityRegistry Capabilities => _capabilities ??= new FlowCapabilityRegistry(this);

    /// <summary>与 <see cref="Capabilities"/> 同义。</summary>
    public FlowCapabilityRegistry Hooks => Capabilities;

    /// <summary>Gate / Hook / Notifier 分发器。</summary>
    public FlowCapabilityDispatcher CapabilityDispatcher =>
        _capabilityDispatcher ??= new FlowCapabilityDispatcher(this);

    /// <summary>Gate / Hook / Outbox 轻量指标（默认 no-op）。</summary>
    public IFlowCapabilityMetrics CapabilityMetrics { get; set; } = NullFlowCapabilityMetrics.Instance;

    /// <summary>Gate / Hook 行为（Sync 硬超时等）。</summary>
    public FlowCapabilityOptions CapabilityOptions { get; set; } = new();

    /// <summary>Outbox Worker / drain 运行参数（<see cref="Hosting.SailHookOutboxDrainExtensions.UseHookOutboxDrain"/> 可覆盖）。</summary>
    public HookOutboxOptions HookOutboxOptions { get; set; } = new();

    /// <summary>
    /// Build 时登记的已装能力快照（UseXxx / ProvideFeature）。宿主探针可读；手动 new 的 Client 为空。
    /// </summary>
    public IReadOnlyList<SailInstalledFeature> InstalledFeatures { get; internal set; } =
        Array.Empty<SailInstalledFeature>();

    /// <summary>是否已登记指定能力名（跨 Phase）。</summary>
    public bool HasFeature(string name) =>
        !string.IsNullOrWhiteSpace(name)
        && InstalledFeatures.Any(f => string.Equals(f.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// 冷热归档 / 冷库 / 引擎日志分表等生命周期参数（默认见 <see cref="SailDataLifecycleOptions"/>）。
    /// Builder：<c>UseDataLifecycle</c>；运行期：<see cref="useDataLifecycle"/>。
    /// </summary>
    public SailDataLifecycleOptions DataLifecycleOptions { get; set; } = new();

    private Logging.IEngineLogSink _logSink;

    /// <summary>
    /// 引擎诊断日志 Sink。默认写表实现在 saiflow.data（<c>UseSailData</c> / <c>useDefaultTableLogSink</c>）。
    /// </summary>
    public Logging.IEngineLogSink LogSink
    {
        get => _logSink;
        set => _logSink = value;
    }

    /// <summary>解析日志 Sink：显式注入优先；未挂载时为空操作（由 saiflow.data 注册 Table Sink）。</summary>
    public Logging.IEngineLogSink GetLogSink() =>
        _logSink ??= Logging.NullEngineLogSink.Instance;

    /// <summary>替换日志 Sink（如 Host ILogger 适配器 / TableEngineLogSink）。</summary>
    public SailClient useLogSink(Logging.IEngineLogSink sink)
    {
        _logSink = sink ?? throw new ArgumentNullException(nameof(sink));
        return this;
    }

    private saiflow.engine.HotShard.ISailHotShardRouter _hotShard;

    /// <summary>
    /// 轨迹/变量热表白名单分表路由（默认 <see cref="saiflow.engine.HotShard.NullSailHotShardRouter"/>）。
    /// 白名单实现由 saiflow.data <c>UseSailData</c> 按 Options 挂载。
    /// </summary>
    public saiflow.engine.HotShard.ISailHotShardRouter HotShard
    {
        get => _hotShard ??= saiflow.engine.HotShard.NullSailHotShardRouter.Instance;
        set => _hotShard = value;
    }

    public SailClient useHotShard(saiflow.engine.HotShard.ISailHotShardRouter router)
    {
        _hotShard = router ?? throw new ArgumentNullException(nameof(router));
        return this;
    }

    /// <summary><see cref="Hooks.HookOutboxDrainLoop.StartBackground"/> 返回的 CTS（<c>autoStart</c> 或宿主手动启动时赋值）。</summary>
    public CancellationTokenSource HookOutboxDrainCts { get; set; }

    /// <summary>R10：Assignee 自动审批锚点 drain CTS（UseAssign.UseAutoApproveJob AutoStart）。</summary>
    public CancellationTokenSource AssigneeAutoApproveDrainCts { get; set; }

    /// <summary>
    /// 规则引擎注册表（按 <c>wf_rule_set.mode</c> 调度）。实现由业务侧 <see cref="useRuleEngine"/> 挂载。
    /// </summary>
    public RuleEngineRegistry RuleEngines => _ruleEngines ??= new RuleEngineRegistry();

    /// <summary>Action 方法注册表（LocalService/SQL/WebApi 等内置；Python 由 sailflow.action 挂载）。</summary>
    public ActionMethodRegistry ActionMethods => _actionMethods ??= new ActionMethodRegistry();

    /// <summary>Script 方法注册表（SQL 内置；Python 由 sailflow.action 挂载）。</summary>
    public ScriptMethodRegistry ScriptMethods => _scriptMethods ??= new ScriptMethodRegistry();

    /// <summary>Service 方法注册表（LocalService/WebApi 等内置）。</summary>
    public ServiceMethodRegistry ServiceMethods => _serviceMethods ??= new ServiceMethodRegistry();

    /// <summary>
    /// 消息队列插槽（Message/Signal Throw 发布）。实现由业务侧 <see cref="useMessageQueue"/> / UseBizMsg 挂载。
    /// 未挂载时为 null；EnsurePorts 不会自动创建。
    /// </summary>
    public IMessageQueueService MessageQueue { get; private set; }

    /// <summary>
    /// JSON 序列化插槽（下一步执行人 / MQ 载荷 / Hook Outbox / 日志等）。
    /// 实现由业务侧 <see cref="useJsonSerializer"/> / UseBizJson 挂载。
    /// </summary>
    public IJsonSerializer JsonSerializer { get; private set; }

    /// <summary>
    /// 办理人管道插槽（查找待办人 / 流出判定）。实现由 <c>UseAssignees</c> / <c>UseBizAssignees</c> 挂载。
    /// 未挂载时为 null；创建待办须挂管道（R2），不再回落 Specific/Definition/Single。
    /// </summary>
    public IAssigneePipeline AssigneePipeline { get; private set; }

    /// <summary>注册消息队列实现（覆盖已有）。</summary>
    public SailClient useMessageQueue(IMessageQueueService messageQueue)
    {
        MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        return this;
    }

    /// <summary>注册 JSON 序列化实现（覆盖已有）。</summary>
    public SailClient useJsonSerializer(IJsonSerializer serializer)
    {
        JsonSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        return this;
    }

    /// <summary>注册办理人管道（覆盖已有）。</summary>
    public SailClient useAssigneePipeline(IAssigneePipeline pipeline)
    {
        AssigneePipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        return this;
    }

    /// <summary>取得已挂载的消息队列；未注册时抛明确异常。</summary>
    public IMessageQueueService GetMessageQueueRequired()
    {
        if (MessageQueue != null)
            return MessageQueue;
        throw new InvalidOperationException(
            "No IMessageQueueService registered on SailClient. Call UseBizMsg() / UseRabbitMq() / useMessageQueue(...).");
    }

    /// <summary>取得已挂载的办理人管道；未注册时抛明确异常。</summary>
    public IAssigneePipeline GetAssigneePipelineRequired()
    {
        if (AssigneePipeline != null)
            return AssigneePipeline;
        throw new InvalidOperationException(
            "No IAssigneePipeline registered on SailClient. Call UseBizAssignees() / UseAssignees() / useAssigneePipeline(...).");
    }

    /// <summary>注册 Gate（同名后注册覆盖）。</summary>
    public SailClient useFlowGate(IFlowGate gate, HookRegistrationOptions options = null)
    {
        Capabilities.RegisterGate(gate, options ?? new HookRegistrationOptions());
        return this;
    }

    /// <summary>注册 Hook（同名后注册覆盖）。</summary>
    public SailClient useFlowHook(IFlowHook hook, HookRegistrationOptions options = null)
    {
        Capabilities.RegisterHook(hook, options ?? new HookRegistrationOptions());
        return this;
    }

    /// <summary>注册待办 Notifier（同名后注册覆盖）。</summary>
    public SailClient useTaskNotifier(ITaskNotifier notifier, HookRegistrationOptions options = null)
    {
        Capabilities.RegisterNotifier(notifier, options ?? new HookRegistrationOptions());
        return this;
    }

    /// <summary>注册规则引擎实现（可多次调用以挂多种 mode）。</summary>
    public SailClient useRuleEngine(IRuleEngine engine)
    {
        RuleEngines.Register(engine);
        return this;
    }

    /// <summary>注册 Action 方法处理器（可覆盖同 Method）。</summary>
    public SailClient useActionMethod(IActionMethodHandler handler)
    {
        ActionMethods.Register(handler);
        return this;
    }

    /// <summary>注册 Script 方法处理器（可覆盖同 Method）。</summary>
    public SailClient useScriptMethod(IScriptMethodHandler handler)
    {
        ScriptMethods.Register(handler);
        return this;
    }

    /// <summary>注册 Service 方法处理器（可覆盖同 Method）。</summary>
    public SailClient useServiceMethod(IServiceMethodHandler handler)
    {
        ServiceMethods.Register(handler);
        return this;
    }

    /// <summary>确保起步插槽已挂载。</summary>
    public void EnsurePorts()
    {
        if (DBFactory == null)
            throw new InvalidOperationException("SailClient.DBFactory is required.");
        if (Dialect == null)
            throw new InvalidOperationException("SailClient.Dialect is required.");
        if (Config == null)
            Config = new SailConfig(this);

        Dialect.MountDbFactory(DBFactory);
        if (Dialect.Event == null)
            Dialect.MountEvent(new SailEventHubStub(this));
        if (_cache == null)
            _cache = CreateDefaultCache();
        if (_ruleEngines == null)
            _ruleEngines = new RuleEngineRegistry();
        if (_actionMethods == null)
            _actionMethods = new ActionMethodRegistry();
        if (_scriptMethods == null)
            _scriptMethods = new ScriptMethodRegistry();
        if (_serviceMethods == null)
            _serviceMethods = new ServiceMethodRegistry();

        BuiltinActionMethods.RegisterAll(_actionMethods, _scriptMethods, _serviceMethods);
    }

    private ISooCache CreateDefaultCache()
    {
        var days = Config?.ExpiredDays > 0 ? Config.ExpiredDays : 1;
        return new MemorySooCache(TimeSpan.FromDays(days));
    }
}
