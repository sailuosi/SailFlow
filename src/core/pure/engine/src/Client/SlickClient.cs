using System;
using mooSQL.data;

namespace saiflow.engine.Client;

/// <summary>
/// 流程运行根实例（对齐 BeeFlow Client 模式）。
/// 具体 <see cref="DBInstance"/> 仅在应用宿主构建时注入，业务层通过 DI 获取本实例。
/// 数据访问终态约定：无事务用 <see cref="UseRunSQL"/>；有事务由上游灌入同一 <see cref="SQLBuilder"/>（begin/commit）。
/// 业务不持有 DBInstance，不依赖 DBExecutor。切片完成须符合约定，禁止中间态。详见 doc/slick/08。
/// </summary>
public class SailClient
{
    public SailDBFactory DBFactory { get; set; } = null!;

    public SailDialect Dialect { get; set; } = null!;

    public SailConfig Config { get; set; } = null!;

    /// <summary>事件插槽便捷访问（与 <see cref="SailDialect.Event"/> 同源）。</summary>
    public ISailEventHub Event => Dialect?.Event
        ?? throw new InvalidOperationException("SailClient.Dialect.Event is not mounted.");

    /// <summary>
    /// 主库实例快捷访问（装配/诊断用）。业务 Manager 请优先 <see cref="UseRunSQL"/>，勿扩散持有 DBInstance。
    /// </summary>
    public DBInstance GetDB() => DBFactory.GetDB();

    /// <summary>运行库 SQLBuilder（每请求独立实例；无事务即取即用，有事务则 begin 后向下传递）。</summary>
    public SQLBuilder UseRunSQL() => DBFactory.useRunSQL();

    /// <summary>确保起步插槽已挂载。</summary>
    public void EnsurePorts()
    {
        if (DBFactory == null)
            throw new InvalidOperationException("SailClient.DBFactory is required.");
        if (Dialect == null)
            throw new InvalidOperationException("SailClient.Dialect is required.");
        if (Config == null)
            throw new InvalidOperationException("SailClient.Config is required.");

        Dialect.MountDbFactory(DBFactory);
        if (Dialect.Event == null)
            Dialect.MountEvent(new SailEventHubStub(this));
    }
}
