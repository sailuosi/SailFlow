using System;

namespace saiflow.engine.Client;

/// <summary>
/// 主方言：起步插槽为「数据库工厂」「事件」。禁止业务向本类型无限继承；扩展用 Register/Mount。
/// </summary>
public sealed class SailDialect
{
    private readonly SailClient _client;

    public SailDialect(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public SailClient Client => _client;

    /// <summary>插槽：数据库工厂。</summary>
    public SailDBFactory DbFactory { get; private set; }

    /// <summary>插槽：事件。</summary>
    public ISailEventHub Event { get; private set; }

    public void MountDbFactory(SailDBFactory factory)
    {
        DbFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public void MountEvent(ISailEventHub eventHub)
    {
        Event = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
    }

    public void RegisterDbFactory(SailDBFactory factory) => MountDbFactory(factory);

    public void RegisterEvent(ISailEventHub eventHub) => MountEvent(eventHub);

    /// <summary>
    /// Phase E：手写 SQL 方言片段入口（表名/视图/TOP·LIMIT）。
    /// 实现位于 <c>Business.SqlProvider.SailSqlDialect</c>，此处仅作 Client 侧发现点。
    /// </summary>
    public static class Sql
    {
        public static bool IsSqlServer => Business.SqlProvider.SailSqlDialect.IsSqlServer;
        public static string ProcessTable => Business.SqlProvider.SailSqlDialect.ProcessTable;
        public static string ProcessEntityTable => Business.SqlProvider.SailSqlDialect.ProcessEntityTable;
        public static string ActivityInstanceTable => Business.SqlProvider.SailSqlDialect.ActivityInstanceTable;
        public static string ProcessInstanceTable => Business.SqlProvider.SailSqlDialect.ProcessInstanceTable;
        public static string TaskTable => Business.SqlProvider.SailSqlDialect.TaskTable;
        public static string VariableTable => Business.SqlProvider.SailSqlDialect.VariableTable;
        public static string TaskView => Business.SqlProvider.SailSqlDialect.TaskView;
        public static string MapField(string entityField) => Business.SqlProvider.SailSqlDialect.MapField(entityField);
        public static string LimitPrefix(int n) => Business.SqlProvider.SailSqlDialect.LimitPrefix(n);
        public static string LimitSuffix(int n) => Business.SqlProvider.SailSqlDialect.LimitSuffix(n);
    }
}
