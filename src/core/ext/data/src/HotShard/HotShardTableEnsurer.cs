using System;
using mooSQL.data;
using sailflow.data.Lifecycle;
using sailflow.engine.Client;
using sailflow.engine.Entity;
using sailflow.engine.HotShard;

namespace sailflow.data.HotShard
{
    /// <summary>
    /// 白名单热分表 ENSURE：优先拷默认表结构；否则按实体
    /// <see cref="TransitionInstanceEntity"/> / <see cref="ProcessVariableEntity"/> 建表。
    /// </summary>
    public static class HotShardTableEnsurer
    {
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
                    EnsureTransition(kit, router.ResolveTransitionTable(processId), router.DefaultTransitionTable);
                    EnsureVariable(kit, router.ResolveVariableTable(processId), router.DefaultVariableTable);
                }
            }
        }

        public static void Ensure(SQLBuilder kit, string shardTable, string templateTable) =>
            EnsureTransition(kit, shardTable, templateTable);

        public static void EnsureTransition(SQLBuilder kit, string shardTable, string templateTable) =>
            TemplateTableEnsurer.Ensure(
                kit,
                shardTable,
                templateTable,
                entityType: typeof(TransitionInstanceEntity),
                configure: TemplateTableEnsurer.ApplySailColumnTags,
                rethrow: true);

        public static void EnsureVariable(SQLBuilder kit, string shardTable, string templateTable) =>
            TemplateTableEnsurer.Ensure(
                kit,
                shardTable,
                templateTable,
                entityType: typeof(ProcessVariableEntity),
                configure: TemplateTableEnsurer.ApplySailColumnTags,
                rethrow: true);
    }
}
