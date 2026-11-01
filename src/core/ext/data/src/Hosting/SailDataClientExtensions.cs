using System;
using System.Runtime.CompilerServices;
using saiflow.data.Lifecycle;
using saiflow.data.Logging;
using saiflow.engine.Client;
using saiflow.engine.Logging;

namespace saiflow.data.Hosting
{
    /// <summary>热→冷归档 / 冷存储 / 默认 Table 日志 Sink 的 Client 扩展。</summary>
    public static class SailDataClientExtensions
    {
        private static readonly ConditionalWeakTable<SailClient, ISailColdStore> ColdStores = new();

        public static ISailColdStore GetColdStore(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            return ColdStores.GetValue(client, static c => new PrimarySailColdStore(c));
        }

        public static SailClient useColdStore(this SailClient client, ISailColdStore coldStore)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(coldStore);
            ColdStores.Remove(client);
            ColdStores.Add(client, coldStore);
            return client;
        }

        /// <summary>按 <see cref="SailDataLifecycleOptions"/> 保留窗执行一批热→冷归档。</summary>
        public static int RunProcessArchiveBatch(this SailClient client, int? batchSize = null) =>
            new ProcessArchiveService(client, client.GetColdStore()).RunBatch(batchSize);

        /// <summary>归档单个终态实例（立刻模式 / Hook 可调）。</summary>
        public static bool ArchiveProcessInstance(this SailClient client, int processInstanceId) =>
            new ProcessArchiveService(client, client.GetColdStore()).ArchiveOne(processInstanceId);

        /// <summary>冷表只读查询入口。</summary>
        public static ProcessArchiveQuery CreateArchiveQuery(this SailClient client) =>
            new ProcessArchiveQuery(client, client.GetColdStore());

        /// <summary>
        /// 按 Options.LogSinkKind 挂默认 Table Sink（已显式 useLogSink 则跳过）。
        /// File / HostLogger / Custom 由宿主自行注入。
        /// </summary>
        public static SailClient useDefaultTableLogSink(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (client.LogSink != null)
                return client;

            var kind = client.DataLifecycleOptions?.LogSinkKind ?? EngineLogSinkKind.Table;
            if (kind == EngineLogSinkKind.Table)
                client.useLogSink(new TableEngineLogSink(client));
            return client;
        }
    }
}
