namespace saiflow.engine.Client;

/// <summary>
/// 冷热归档 / 冷库 / 引擎日志落点等与「性能分表」相关的运行参数。
/// 带默认值；由 <see cref="SailClient.DataLifecycleOptions"/> 持有。
/// 引擎不解析 IConfiguration——宿主可从配置文件绑定后经 Builder / <c>useDataLifecycle</c> 写入。
/// </summary>
public sealed class SailDataLifecycleOptions
{
    public const int DefaultArchiveRetentionDays = 90;
    public const int DefaultArchiveBatchSize = 100;
    public const int DefaultLogRetentionDays = 90;

    // --- 流程实例归档 / 冷表 ---

    /// <summary>终态实例保留在热表的天数（按 ended_datetime）；默认 90。</summary>
    public int ArchiveRetentionDays { get; set; } = DefaultArchiveRetentionDays;

    /// <summary>单次归档作业最多处理的实例数。</summary>
    public int ArchiveBatchSize { get; set; } = DefaultArchiveBatchSize;

    /// <summary>
    /// 冷表名模式：{0}=逻辑基名（如 process_instance / task），{1}=办结年 yyyy。
    /// 例：wf_task_hist_2026。
    /// </summary>
    public string HistTableNameFormat { get; set; } = "wf_{0}_hist_{1}";

    /// <summary>冷存储是否使用与热库同一连接（默认 true）。</summary>
    public bool ColdStoreUsePrimaryDatabase { get; set; } = true;

    /// <summary>
    /// 冷库连接名 / 工厂键（当 <see cref="ColdStoreUsePrimaryDatabase"/> 为 false 时使用）。
    /// 具体如何解析由宿主 / DBFactory 约定。
    /// </summary>
    public string ColdStoreConnectionName { get; set; }

    // --- 引擎诊断日志（默认 Table Sink） ---

    /// <summary>日志 Sink 类型；默认写表。可改为 File / HostLogger / Custom。</summary>
    public EngineLogSinkKind LogSinkKind { get; set; } = EngineLogSinkKind.Table;

    /// <summary>默认 Table Sink 分表粒度；默认按月（与流程冷表按年独立）。</summary>
    public EngineLogShardGranularity LogShardGranularity { get; set; } = EngineLogShardGranularity.Month;

    /// <summary>日志是否写热库同连接（默认 true；可指到 SQLite / 其它库）。</summary>
    public bool LogUsePrimaryDatabase { get; set; } = true;

    /// <summary>日志库连接名（当 <see cref="LogUsePrimaryDatabase"/> 为 false）。</summary>
    public string LogConnectionName { get; set; }

    /// <summary>日志表基名（分表时加后缀，如 wf_log_202611）。</summary>
    public string LogTableBaseName { get; set; } = "wf_log";

    /// <summary>作业日志表基名。</summary>
    public string JobLogTableBaseName { get; set; } = "wf_job_log";

    /// <summary>日志保留天数（Table Sink TTL）；默认 90。</summary>
    public int LogRetentionDays { get; set; } = DefaultLogRetentionDays;

    /// <summary>从另一实例拷贝字段（Builder / 配置合并用）。</summary>
    public void Apply(SailDataLifecycleOptions source)
    {
        if (source == null)
            return;

        ArchiveRetentionDays = source.ArchiveRetentionDays;
        ArchiveBatchSize = source.ArchiveBatchSize;
        if (source.HistTableNameFormat != null)
            HistTableNameFormat = source.HistTableNameFormat;
        ColdStoreUsePrimaryDatabase = source.ColdStoreUsePrimaryDatabase;
        if (source.ColdStoreConnectionName != null)
            ColdStoreConnectionName = source.ColdStoreConnectionName;

        LogSinkKind = source.LogSinkKind;
        LogShardGranularity = source.LogShardGranularity;
        LogUsePrimaryDatabase = source.LogUsePrimaryDatabase;
        if (source.LogConnectionName != null)
            LogConnectionName = source.LogConnectionName;
        if (source.LogTableBaseName != null)
            LogTableBaseName = source.LogTableBaseName;
        if (source.JobLogTableBaseName != null)
            JobLogTableBaseName = source.JobLogTableBaseName;
        LogRetentionDays = source.LogRetentionDays;
    }

    /// <summary>按办结年解析冷表物理名。</summary>
    public string FormatHistTableName(string logicalBaseName, int endedYear)
        => string.Format(HistTableNameFormat, logicalBaseName, endedYear);
}

/// <summary>引擎诊断日志 Sink 种类。</summary>
public enum EngineLogSinkKind
{
    /// <summary>写库表（默认可分表）；现行行为。</summary>
    Table = 0,

    /// <summary>写文件（实现由宿主或扩展包提供）。</summary>
    File = 1,

    /// <summary>转发 Host / Microsoft.Extensions.Logging。</summary>
    HostLogger = 2,

    /// <summary>自定义已注册 Sink。</summary>
    Custom = 3
}

/// <summary>默认 Table Sink 的分表粒度。</summary>
public enum EngineLogShardGranularity
{
    /// <summary>单表 wf_log（无后缀）。</summary>
    None = 0,

    /// <summary>按月：wf_log_yyyyMM。</summary>
    Month = 1,

    /// <summary>按年：wf_log_yyyy。</summary>
    Year = 2
}
