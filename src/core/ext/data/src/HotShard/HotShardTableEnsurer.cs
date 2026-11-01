using System;
using System.Collections.Concurrent;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.HotShard;

namespace saiflow.data.HotShard
{
    /// <summary>白名单热分表 ENSURE（从默认表零行拷结构）。</summary>
    public static class HotShardTableEnsurer
    {
        private static readonly ConcurrentDictionary<string, byte> Ensured =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public static void EnsureAll(SailClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var opts = client.DataLifecycleOptions;
            if (opts?.HotShardProcessIds == null || opts.HotShardProcessIds.Count == 0)
                return;

            var router = client.HotShard as LifecycleHotShardRouter
                ?? new LifecycleHotShardRouter(client);

            using (var kit = client.UseRunSQL())
            {
                foreach (var processId in opts.HotShardProcessIds)
                {
                    if (string.IsNullOrWhiteSpace(processId))
                        continue;
                    Ensure(kit, router.ResolveTransitionTable(processId), router.DefaultTransitionTable);
                    Ensure(kit, router.ResolveVariableTable(processId), router.DefaultVariableTable);
                }
            }
        }

        public static void Ensure(SQLBuilder kit, string shardTable, string templateTable)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));
            if (string.IsNullOrWhiteSpace(shardTable) || string.IsNullOrWhiteSpace(templateTable))
                return;
            if (string.Equals(shardTable, templateTable, StringComparison.OrdinalIgnoreCase))
                return;
            if (!Ensured.TryAdd(shardTable, 0))
                return;

            try
            {
                kit.exeNonQuery(
                    "CREATE TABLE IF NOT EXISTS " + shardTable
                    + " AS SELECT * FROM " + templateTable + " WHERE 0=1");
            }
            catch
            {
                Ensured.TryRemove(shardTable, out _);
                throw;
            }
        }
    }
}
