using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using mooSQL.data;
using saiflow.AI.Entity;
using saiflow.engine.Business.Entity;
using saiflow.hub.Logging;
using saiflow.hub.Resource;

namespace saiflow.engine.Client
{
    /// <summary>
    /// 任务 1½：基于 mooSQL <c>useDBInit</c> 建表与最小种子（Host/探活调用；不进 Dialect）。
    /// 默认 <c>autoMode</c>：表不存在则建，存在则补字段；不 DROP。
    /// </summary>
    public static class SailSchemaInitializer
    {
        /// <summary>P0 + Resource + 常用附属表（不含 View、不含同表 ProcessFile）。</summary>
        public static string EnsureTables(SailClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return EnsureTables(client.UseRunSQL());
        }

        public static string EnsureTables(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            var wk = kit.useDBInit();
            wk.autoMode()
                .createTable<ProcessEntity>()
                .createTable<ProcessInstanceEntity>()
                .createTable<ActivityInstanceEntity>()
                .createTable<TaskEntity>()
                .createTable<TransitionInstanceEntity>()
                .createTable<VariableEntity>()
                .createTable<ProcessVariableEntity>()
                .createTable<LogEntity>()
                .createTable<JobInfoEntity>()
                .createTable<RuleSetEntity>()
                .createTable<JobLogEntity>()
                .createTable<UserEntity>()
                .createTable<RoleEntity>()
                .createTable<RoleUserEntity>()
                .createTable<DeptEntity>()
                .createTable<EmpEntity>()
                .createTable<EmpManagerEntity>()
                .createTable<AiModelProviderEntity>()
                .createTable<AiActivityConfigEntity>();

            return wk.WorkingProgress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 空表时插入 1 条流程元数据（不含完整 BPMN）；可重复调用。
        /// </summary>
        public static int EnsureMinimalSeed(SailClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return EnsureMinimalSeed(client.UseRunSQL());
        }

        public static int EnsureMinimalSeed(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            if (kit.countBy<ProcessEntity>() > 0)
                return 0;

            var now = DateTime.UtcNow;
            var rows = new List<ProcessEntity>
            {
                new ProcessEntity
                {
                    ProcessId = "slick-seed-hello",
                    ProcessName = "Sail Seed Hello",
                    ProcessCode = "SEED_HELLO",
                    Version = "1",
                    Status = 1,
                    AppType = "seed",
                    XmlFileName = "seed-hello.xml",
                    XmlContent = MinimalBpmnXml(),
                    StartType = 0,
                    EndType = 0,
                    Description = "任务 1½ 最小种子（可删）",
                    CreatedDateTime = now
                }
            };

            // SQLite 文本日期：避免 CurrentCulture（如 zh-CN）写入「yyyy/M/d/周一 …」导致回读解析失败
            var prev = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                return kit.insertList(rows);
            }
            finally
            {
                CultureInfo.CurrentCulture = prev;
                Thread.CurrentThread.CurrentCulture = prev;
            }
        }

        /// <summary>建表 + 可选种子；返回进度文本。</summary>
        public static SailInitDbResult InitDb(SailClient client, bool seed = true)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var kit = client.UseRunSQL();
            var progress = EnsureTables(kit);
            var seeded = seed ? EnsureMinimalSeed(kit) : 0;
            return new SailInitDbResult
            {
                Progress = progress,
                SeededRows = seeded
            };
        }

        private static string MinimalBpmnXml()
        {
            return
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Package><Process id=\"slick-seed-hello\" name=\"Sail Seed Hello\"></Process></Package>";
        }
    }

    public sealed class SailInitDbResult
    {
        public string Progress { get; set; }
        public int SeededRows { get; set; }
    }
}
