using System;
using mooSQL.data;
using saiflow.engine.Actions;
using saiflow.engine.Rules;
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

    private RuleEngineRegistry _ruleEngines;
    private ActionMethodRegistry _actionMethods;
    private ScriptMethodRegistry _scriptMethods;
    private ServiceMethodRegistry _serviceMethods;

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
