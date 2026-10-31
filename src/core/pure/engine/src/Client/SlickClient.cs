using System;
using mooSQL.data;
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
    }

    private ISooCache CreateDefaultCache()
    {
        var days = Config?.ExpiredDays > 0 ? Config.ExpiredDays : 1;
        return new MemorySooCache(TimeSpan.FromDays(days));
    }
}
