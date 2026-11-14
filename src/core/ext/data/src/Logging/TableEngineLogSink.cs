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
            InsertEntity(table, opts.LogTableBaseName, entry, MapLogColumns);
        }

        public void WriteJob(JobLogEntity entry)
        {
            if (entry == null)
                return;

            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            var when = entry.CreatedDateTime ?? DateTime.UtcNow;
            var table = ResolveShardTableName(opts.JobLogTableBaseName, opts.LogShardGranularity, when);
            InsertEntity(table, opts.JobLogTableBaseName, entry, MapJobLogColumns);
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

        private void InsertEntity<T>(
            string physicalTable,
            string templateTable,
            T entity,
            Action<SQLBuilder, T> mapColumns)
        {
            using (var kit = ResolveKit())
            {
                // 优先拷基表结构；基表不在则按实体建；失败不挡主流程
                TemplateTableEnsurer.Ensure(
                    kit,
                    physicalTable,
                    templateTable,
                    entityType: typeof(T),
                    configure: TemplateTableEnsurer.ApplySailColumnTags,
                    rethrow: false);
                kit.setTable(physicalTable).newRow();
                mapColumns(kit, entity);
                kit.doInsert();
            }
        }

        private SQLBuilder ResolveKit()
        {
            var opts = Client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            // 非主库连接名：待 DBFactory 多连接扩展；当前仍走主库并允许配置占位。
            _ = opts.LogUsePrimaryDatabase;
            _ = opts.LogConnectionName;
            return Client.UseRunSQL();
        }

        private static void MapLogColumns(SQLBuilder kit, LogEntity log)
        {
            if (log.EventTypeId.HasValue) kit.set("lg_evt_type_id", log.EventTypeId.Value);
            if (log.Priority.HasValue) kit.set("lg_priority", log.Priority.Value);
            if (log.Severity != null) kit.set("lg_severity", log.Severity);
            if (log.Title != null) kit.set("lg_title", log.Title);
            if (log.Message != null) kit.set("lg_message", log.Message);
            if (log.StackTrace != null) kit.set("lg_stk_trc", log.StackTrace);
            if (log.InnerStackTrace != null) kit.set("lg_in_stk_trc", log.InnerStackTrace);
            if (log.RequestData != null) kit.set("lg_req_data", log.RequestData);
            if (log.Timestamp.HasValue) kit.set("lg_timestamp", log.Timestamp.Value);
        }

        private static void MapJobLogColumns(SQLBuilder kit, JobLogEntity log)
        {
            if (log.JobType != null) kit.set("jl_job_type", log.JobType);
            if (log.JobName != null) kit.set("jl_job_name", log.JobName);
            if (log.JobKey != null) kit.set("jl_job_key", log.JobKey);
            if (log.RefClass != null) kit.set("jl_ref_cls", log.RefClass);
            if (log.RefIDs != null) kit.set("jl_ref_ids", log.RefIDs);
            if (log.Status.HasValue) kit.set("jl_status", log.Status.Value);
            if (log.Message != null) kit.set("jl_message", log.Message);
            if (log.StackTrace != null) kit.set("jl_stk_trc", log.StackTrace);
            if (log.InnerStackTrace != null) kit.set("jl_in_stk_trc", log.InnerStackTrace);
            if (log.RequestData != null) kit.set("jl_req_data", log.RequestData);
            if (log.CreatedDateTime.HasValue) kit.set("jl_crt_time", log.CreatedDateTime.Value);
            if (log.CreatedUserId != null) kit.set("jl_crt_user_id", log.CreatedUserId);
            if (log.CreatedUserName != null) kit.set("jl_crt_user_name", log.CreatedUserName);
        }
    }
}
