using System;
using mooSQL.data;
using sailflow.data;
using sailflow.hosting;
using sailflow.log;
using sailflow.hub.Logging;
using sailflow.storage;

namespace sailflow.data.Logging
{
    /// <summary>
    /// 默认日志 Sink：写库表。库连接 / 分表粒度读 <see cref="SailDataLifecycleOptions"/>。
    /// SQL 经 <see cref="IEngineLogStorage"/>（须 <c>UseDefaultStorages</c>）。
    /// </summary>
    public sealed class TableEngineLogSink : IEngineLogSink
    {
        public TableEngineLogSink(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public SailClient Client { get; }

        IEngineLogStorage Storage => Client.Storages.GetRequired<IEngineLogStorage>();

        public void Write(LogEntity entry)
        {
            if (entry == null)
                return;

            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            var when = entry.Timestamp ?? DateTime.UtcNow;
            var table = ResolveShardTableName(opts.LogTableBaseName, opts.LogShardGranularity, when);
            using (var kit = ResolveKit())
                Storage.InsertLog(kit, entry, table, opts.LogTableBaseName);
        }

        public void WriteJob(JobLogEntity entry)
        {
            if (entry == null)
                return;

            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            var when = entry.CreatedDateTime ?? DateTime.UtcNow;
            var table = ResolveShardTableName(opts.JobLogTableBaseName, opts.LogShardGranularity, when);
            using (var kit = ResolveKit())
                Storage.InsertJobLog(kit, entry, table, opts.JobLogTableBaseName);
        }

        public static string ResolveShardTableName(
            string baseName,
            EngineLogShardGranularity granularity,
            DateTime utc)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "sf_log";

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

        private SQLBuilder ResolveKit()
        {
            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            _ = opts.LogUsePrimaryDatabase;
            _ = opts.LogConnectionName;
            return Client.UseRunSQL();
        }
    }
}
