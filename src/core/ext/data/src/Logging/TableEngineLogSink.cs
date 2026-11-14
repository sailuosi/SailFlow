using System;
using mooSQL.data;
using sailflow.data.Lifecycle;
using sailflow.engine.Entity;
using sailflow.engine.Client;
using sailflow.engine.Logging;
using sailflow.hub.Logging;

namespace sailflow.data.Logging
{
    /// <summary>
    /// 默认日志 Sink：写库表。库连接 / 分表粒度读 <see cref="SailDataLifecycleOptions"/>。
    /// 一律<strong>实体插入</strong>（触发 AutoKey=SailId），禁止手工 set 列。
    /// </summary>
    public sealed class TableEngineLogSink : IEngineLogSink
    {
        public TableEngineLogSink(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public SailClient Client { get; }

        public void Write(LogEntity entry)
        {
            if (entry == null)
                return;

            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            var when = entry.Timestamp ?? DateTime.UtcNow;
            var table = ResolveShardTableName(opts.LogTableBaseName, opts.LogShardGranularity, when);
            InsertByEntity(table, opts.LogTableBaseName, entry);
        }

        public void WriteJob(JobLogEntity entry)
        {
            if (entry == null)
                return;

            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            var when = entry.CreatedDateTime ?? DateTime.UtcNow;
            var table = ResolveShardTableName(opts.JobLogTableBaseName, opts.LogShardGranularity, when);
            InsertByEntity(table, opts.JobLogTableBaseName, entry);
        }

        public static string ResolveShardTableName(
            string baseName,
            EngineLogShardGranularity granularity,
            DateTime utc)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "wf_log";

            switch (granularity)
            {
                case EngineLogShardGranularity.Month:
                    return baseName + "_" + utc.ToString("yyyyMM");
                case EngineLogShardGranularity.Year:
                    return baseName + "_" + utc.ToString("yyyy");
                default:
                    return baseName;
            }
        }

        /// <summary>
        /// 实体写入：基表用 <c>useRepo&lt;T&gt;().Insert</c>；分表用 <c>insert(entity, physicalTable)</c>。
        /// </summary>
        private void InsertByEntity<T>(string physicalTable, string templateTable, T entity)
            where T : class, new()
        {
            using (var kit = ResolveKit())
            {
                TemplateTableEnsurer.Ensure(
                    kit,
                    physicalTable,
                    templateTable,
                    entityType: typeof(T),
                    configure: TemplateTableEnsurer.ApplySailColumnTags,
                    rethrow: false);

                var useBaseTable = string.IsNullOrWhiteSpace(physicalTable)
                    || string.Equals(physicalTable, templateTable, StringComparison.OrdinalIgnoreCase);

                if (useBaseTable)
                {
                    if (!kit.useRepo<T>().Insert(entity))
                        throw new InvalidOperationException(
                            "TableEngineLogSink repo.Insert failed for " + typeof(T).Name);
                    return;
                }

                // 分表物理名：仍走实体 prepareInsert（含 AutoKey），仅覆盖表名
                var n = kit.insert(entity, physicalTable);
                if (n < 0)
                    throw new InvalidOperationException(
                        "TableEngineLogSink entity insert failed for table " + physicalTable);
            }
        }

        private SQLBuilder ResolveKit()
        {
            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            _ = opts.LogUsePrimaryDatabase;
            _ = opts.LogConnectionName;
            return Client.UseRunSQL();
        }
    }
}
