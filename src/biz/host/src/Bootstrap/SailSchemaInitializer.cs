using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using mooSQL.data;
using saiflow.AI.Runtime;
using saiflow.data.Schema;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.biz.query;
using saiflow.hub.Form;
using saiflow.hub.Logging;

namespace saiflow.biz.Bootstrap
{
    /// <summary>
    /// Business-side DB bootstrap: core engine tables + optional AI tables + minimal seed.
    /// Hosts should call this instead of anything in the engine assembly.
    /// </summary>
    public static class SailSchemaInitializer
    {
        /// <summary>
        /// Core workflow / resource tables (no AI).
        /// </summary>
        public static string EnsureCoreTables(SailClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return EnsureCoreTables(client.UseRunSQL());
        }

        public static string EnsureCoreTables(SQLBuilder kit)
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
                .createTable<HookOutboxEntity>()
                // 组织人员 / 角色用户由宿主主数据提供，引擎不再建 sys_* 表
                .createTable<FormEntity>()
                .createTable<FieldActivityEditEntity>()
                .createTable<SailProcessCopyEntity>();

            EnsureAssigneeGroupColumns(kit);
            EnsureAssigneeUnitColumns(kit);
            HotPathIndexEnsurer.Ensure(kit);
            EnsureTaskDetailsView(kit);
            return wk.WorkingProgress?.ToString() ?? string.Empty;
        }

        /// <summary>S2：办理人组别/组内身份列（已有库用 ALTER 增量）。</summary>
        public static void EnsureAssigneeGroupColumns(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            EnsureSqliteColumn(kit, "wf_task", "group_id", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_task", "group_name", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_task", "group_role", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "group_id", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "group_name", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "group_role", "TEXT NULL");
        }

        /// <summary>R1：企业单位 Unit* / 发起单位 StartUnit*（已有库 ALTER 增量）。</summary>
        public static void EnsureAssigneeUnitColumns(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            EnsureSqliteColumn(kit, "wf_task", "unit_id", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_task", "unit_code", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_task", "unit_name", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "unit_id", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "unit_code", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_activity_instance", "unit_name", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_process_instance", "start_unit_id", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_process_instance", "start_unit_code", "TEXT NULL");
            EnsureSqliteColumn(kit, "wf_process_instance", "start_unit_name", "TEXT NULL");
        }

        static void EnsureSqliteColumn(SQLBuilder kit, string tableName, string columnName, string columnType)
        {
            try
            {
                var info = kit.exeQuery("PRAGMA table_info(" + tableName + ")");
                for (var i = 0; i < info.Rows.Count; i++)
                {
                    var name = Convert.ToString(info.Rows[i]["name"]);
                    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                kit.exeNonQuery("ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + columnType);
            }
            catch
            {
                // 非 SQLite 或缺表时忽略；createTable 会建齐
            }
        }

        /// <summary>
        /// 待办/已办依赖的任务明细视图（任务 ⊕ 活动实例 ⊕ 流程实例）。
        /// Clip 按 View 名查询；库中无此视图时会报 no such table。
        /// </summary>
        public static void EnsureTaskDetailsView(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            try { kit.exeNonQuery("DROP VIEW IF EXISTS vw_wf_task_details"); } catch { /* ignore */ }

            const string sql = @"
CREATE VIEW IF NOT EXISTS vw_wf_task_details AS
SELECT
  t.id AS id,
  t.app_name AS app_name,
  t.app_instance_id AS app_instance_id,
  ai.app_instance_code AS app_instance_code,
  t.process_instance_id AS process_instance_id,
  t.process_id AS process_id,
  pi.version AS version,
  t.activity_id AS activity_id,
  t.activity_instance_id AS activity_instance_id,
  t.activity_name AS activity_name,
  ai.activity_code AS activity_code,
  ai.activity_type AS activity_type,
  ai.work_item_type AS work_item_type,
  t.task_type AS task_type,
  pi.sub_process_type AS sub_process_type,
  pi.sub_process_def_id AS sub_process_def_id,
  pi.""sub_process_Id"" AS sub_process_id,
  t.entrusted_task_id AS entrusted_task_id,
  ai.main_activity_instance_id AS main_activity_instance_id,
  ai.complete_order AS complete_order,
  t.assigned_user_id AS assigned_user_id,
  t.assigned_user_name AS assigned_user_name,
  t.group_id AS group_id,
  t.group_name AS group_name,
  t.group_role AS group_role,
  t.unit_id AS unit_id,
  t.unit_code AS unit_code,
  t.unit_name AS unit_name,
  t.is_email_sent AS is_email_sent,
  ai.approval_status AS approval_status,
  t.created_datetime AS created_datetime,
  t.updated_datetime AS updated_datetime,
  t.ended_datetime AS ended_datetime,
  t.ended_user_id AS ended_user_id,
  t.ended_user_name AS ended_user_name,
  t.task_state AS task_state,
  ai.activity_state AS activity_state,
  mai.activity_state AS main_activity_state,
  t.record_status_invalid AS record_status_invalid,
  pi.process_state AS process_state
FROM wf_task t
INNER JOIN wf_activity_instance ai ON t.activity_instance_id = ai.id
INNER JOIN wf_process_instance pi ON t.process_instance_id = pi.id
LEFT JOIN wf_activity_instance mai ON ai.main_activity_instance_id = mai.id";

            kit.exeNonQuery(sql);
        }

        /// <summary>Core + AI tables.</summary>
        public static string EnsureTables(SailClient client, bool includeAi = true)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            return EnsureTables(client.UseRunSQL(), includeAi);
        }

        public static string EnsureTables(SQLBuilder kit, bool includeAi = true)
        {
            var progress = EnsureCoreTables(kit);
            if (includeAi)
                progress += AiRuntime.EnsureTables(kit);
            return progress;
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
                    Id = 1,
                    ProcessId = "sail-seed-hello",
                    ProcessName = "Sail Seed Hello",
                    ProcessCode = "SEED_HELLO",
                    Version = "1",
                    Status = 1,
                    AppType = "seed",
                    XmlFileName = "seed-hello.xml",
                    XmlContent = MinimalBpmnXml(),
                    StartType = 0,
                    EndType = 0,
                    Description = "最小种子（可删）",
                    CreatedDateTime = now
                }
            };

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

        /// <summary>
        /// Full host bootstrap: tables (+ optional AI), seed, and default AI executor registration.
        /// </summary>
        public static SailInitDbResult InitDb(SailClient client, bool seed = true, bool includeAi = true, bool registerAiExecutor = true)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var kit = client.UseRunSQL();
            var progress = EnsureTables(kit, includeAi);
            var seeded = seed ? EnsureMinimalSeed(kit) : 0;

            if (includeAi && registerAiExecutor)
                AiRuntime.UseDefaultExecutor(client);

            return new SailInitDbResult
            {
                Progress = progress,
                SeededRows = seeded
            };
        }

        private static string MinimalBpmnXml()
        {
            // bpmn-js 可解析的最小 BPMN 2.0（勿用旧 Package/XPDL 壳）
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<bpmn:definitions xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\" " +
                "xmlns:bpmndi=\"http://www.omg.org/spec/BPMN/20100524/DI\" " +
                "xmlns:dc=\"http://www.omg.org/spec/DD/20100524/DC\" " +
                "xmlns:sf=\"http://www.slickflow.com/schema/sf\" " +
                "id=\"Definitions_SailSeed\" targetNamespace=\"http://bpmn.io/schema/bpmn\">" +
                "<bpmn:process id=\"Process_1\" name=\"Sail Seed Hello\" isExecutable=\"true\" " +
                "sf:code=\"SEED_HELLO\" sf:version=\"1\">" +
                "<bpmn:startEvent id=\"StartEvent_1\" name=\"Start\" />" +
                "</bpmn:process>" +
                "<bpmndi:BPMNDiagram id=\"BPMNDiagram_1\">" +
                "<bpmndi:BPMNPlane id=\"BPMNPlane_1\" bpmnElement=\"Process_1\">" +
                "<bpmndi:BPMNShape id=\"StartEvent_1_di\" bpmnElement=\"StartEvent_1\">" +
                "<dc:Bounds x=\"180\" y=\"160\" width=\"36\" height=\"36\" />" +
                "</bpmndi:BPMNShape>" +
                "</bpmndi:BPMNPlane>" +
                "</bpmndi:BPMNDiagram>" +
                "</bpmn:definitions>";
        }
    }

    public sealed class SailInitDbResult
    {
        public string Progress { get; set; } = string.Empty;
        public int SeededRows { get; set; }
    }
}
