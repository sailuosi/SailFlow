using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using mooSQL.data;
using saiflow.AI.Runtime;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.biz.query;
using saiflow.hub.Form;
using saiflow.hub.Logging;
using saiflow.hub.Resource;

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
                .createTable<UserEntity>()
                .createTable<RoleEntity>()
                .createTable<RoleUserEntity>()
                .createTable<DeptEntity>()
                .createTable<EmpEntity>()
                .createTable<EmpManagerEntity>()
                .createTable<FormEntity>()
                .createTable<FieldActivityEditEntity>()
                .createTable<SailProcessCopyEntity>();

            EnsureFormTables(kit);
            EnsureRuntimeIdentityTables(kit);
            EnsureTaskDetailsView(kit);
            return wk.WorkingProgress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// SQLite 下 mooSQL createTable 流式 DDL 不会落地 INTEGER PRIMARY KEY AUTOINCREMENT
        ///（buildConstrainPK 默认为空），导致 Insert 跳过 Id 后 NOT NULL 失败。
        /// 此处用原生 DDL 保证自增主键；有数据时迁移保留。
        /// </summary>
        public static void EnsureFormTables(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            EnsureSqliteIdentityTable(
                kit,
                "FbForm",
                @"CREATE TABLE FbForm (
  Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  FormName TEXT NULL,
  FormCode TEXT NULL,
  Version TEXT NULL,
  FieldSummary TEXT NULL,
  TemplateContent TEXT NULL,
  HTMLContent TEXT NULL,
  Description TEXT NULL,
  CreatedDate TEXT NULL,
  LastUpdatedDate TEXT NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "FbFormFieldActivityEdit",
                @"CREATE TABLE FbFormFieldActivityEdit (
  Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  ProcessDefId INTEGER NULL,
  ProcessId TEXT NULL,
  ProcessName TEXT NULL,
  ProcessVersion TEXT NULL,
  ActivityId TEXT NULL,
  ActivityName TEXT NULL,
  FormId INTEGER NULL,
  FormName TEXT NULL,
  FormVersion TEXT NULL,
  FieldsPermission TEXT NULL
)");
        }

        /// <summary>运行时自增表（发起/办理会写入）。</summary>
        public static void EnsureRuntimeIdentityTables(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            // 视图依赖任务表，重建前先卸掉
            try { kit.exeNonQuery("DROP VIEW IF EXISTS vw_wf_task_details"); } catch { /* ignore */ }

            EnsureSqliteIdentityTable(
                kit,
                "wf_process",
                @"CREATE TABLE wf_process (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  process_id TEXT NULL,
  process_name TEXT NULL,
  process_code TEXT NULL,
  version TEXT NULL,
  status INTEGER NULL,
  app_type TEXT NULL,
  package_type INTEGER NULL,
  package_id INTEGER NULL,
  participant_guid TEXT NULL,
  page_url TEXT NULL,
  xml_file_name TEXT NULL,
  xml_file_path TEXT NULL,
  xml_content TEXT NULL,
  start_type INTEGER NULL,
  start_expression TEXT NULL,
  end_type INTEGER NULL,
  end_expression TEXT NULL,
  icon TEXT NULL,
  description TEXT NULL,
  created_datetime TEXT NULL,
  updated_datetime TEXT NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_process_instance",
                @"CREATE TABLE wf_process_instance (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  process_id TEXT NULL,
  process_name TEXT NULL,
  version TEXT NULL,
  app_name TEXT NULL,
  app_instance_id TEXT NULL,
  app_instance_code TEXT NULL,
  process_state INTEGER NULL,
  sub_process_type INTEGER NULL,
  sub_process_def_id INTEGER NULL,
  sub_process_Id TEXT NULL,
  invoked_activity_instance_id INTEGER NULL,
  invoked_activity_id TEXT NULL,
  job_timer_type INTEGER NULL,
  job_timer_status INTEGER NULL,
  trigger_expression TEXT NULL,
  overdue_datetime TEXT NULL,
  job_timer_treated_datetime TEXT NULL,
  start_unit_id TEXT NULL,
  start_unit_code TEXT NULL,
  start_unit_name TEXT NULL,
  created_datetime TEXT NULL,
  created_user_id TEXT NULL,
  created_user_name TEXT NULL,
  updated_datetime TEXT NULL,
  updated_user_id TEXT NULL,
  updated_user_name TEXT NULL,
  ended_datetime TEXT NULL,
  ended_user_id TEXT NULL,
  ended_user_name TEXT NULL,
  record_status_invalid INTEGER NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_activity_instance",
                @"CREATE TABLE wf_activity_instance (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  process_instance_id INTEGER NULL,
  app_name TEXT NULL,
  app_instance_id TEXT NULL,
  app_instance_code TEXT NULL,
  process_id TEXT NULL,
  activity_id TEXT NULL,
  activity_name TEXT NULL,
  activity_code TEXT NULL,
  activity_type INTEGER NULL,
  activity_state INTEGER NULL,
  work_item_type INTEGER NULL,
  assigned_user_ids TEXT NULL,
  assigned_user_names TEXT NULL,
  group_id TEXT NULL,
  group_name TEXT NULL,
  group_role TEXT NULL,
  unit_id TEXT NULL,
  unit_code TEXT NULL,
  unit_name TEXT NULL,
  gateway_direction_type_id INTEGER NULL,
  cannot_renew_instance INTEGER NULL,
  approval_status INTEGER NULL,
  tokens_required INTEGER NULL,
  tokens_had INTEGER NULL,
  job_timer_type INTEGER NULL,
  job_timer_status INTEGER NULL,
  trigger_expression TEXT NULL,
  overdue_datetime TEXT NULL,
  job_timer_treated_datetime TEXT NULL,
  complex_type INTEGER NULL,
  merge_type INTEGER NULL,
  main_activity_instance_id INTEGER NULL,
  compare_type INTEGER NULL,
  complete_order REAL NULL,
  sign_forward_type INTEGER NULL,
  backward_type INTEGER NULL,
  back_src_activity_instance_id INTEGER NULL,
  back_org_activity_instance_id INTEGER NULL,
  next_step_performers TEXT NULL,
  created_user_id TEXT NULL,
  created_user_name TEXT NULL,
  created_datetime TEXT NULL,
  updated_user_id TEXT NULL,
  updated_user_name TEXT NULL,
  updated_datetime TEXT NULL,
  ended_datetime TEXT NULL,
  ended_user_id TEXT NULL,
  ended_user_name TEXT NULL,
  record_status_invalid INTEGER NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_task",
                @"CREATE TABLE wf_task (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  activity_instance_id INTEGER NULL,
  process_instance_id INTEGER NULL,
  app_name TEXT NULL,
  app_instance_id TEXT NULL,
  process_id TEXT NULL,
  activity_id TEXT NULL,
  activity_name TEXT NULL,
  task_type INTEGER NULL,
  task_state INTEGER NULL,
  entrusted_task_id INTEGER NULL,
  assigned_user_id TEXT NULL,
  assigned_user_name TEXT NULL,
  group_id TEXT NULL,
  group_name TEXT NULL,
  group_role TEXT NULL,
  unit_id TEXT NULL,
  unit_code TEXT NULL,
  unit_name TEXT NULL,
  is_email_sent INTEGER NULL,
  created_user_id TEXT NULL,
  created_user_name TEXT NULL,
  created_datetime TEXT NULL,
  updated_datetime TEXT NULL,
  updated_user_id TEXT NULL,
  updated_user_name TEXT NULL,
  ended_user_id TEXT NULL,
  ended_user_name TEXT NULL,
  ended_datetime TEXT NULL,
  record_status_invalid INTEGER NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_transition_instance",
                @"CREATE TABLE wf_transition_instance (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  transition_id TEXT NULL,
  app_name TEXT NULL,
  app_instance_id TEXT NULL,
  process_instance_id INTEGER NULL,
  process_id TEXT NULL,
  transition_type INTEGER NULL,
  flying_type INTEGER NULL,
  from_activity_instance_id INTEGER NULL,
  from_activity_id TEXT NULL,
  from_activity_type INTEGER NULL,
  from_activity_name TEXT NULL,
  to_activity_instance_id INTEGER NULL,
  to_activity_id TEXT NULL,
  to_activity_type INTEGER NULL,
  to_activity_name TEXT NULL,
  condition_parsed_result INTEGER NULL,
  created_user_id TEXT NULL,
  created_user_name TEXT NULL,
  created_datetime TEXT NULL,
  record_status_invalid INTEGER NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_process_variable",
                @"CREATE TABLE wf_process_variable (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  variable_scope TEXT NULL,
  app_instance_id TEXT NULL,
  process_id TEXT NULL,
  process_instance_id INTEGER NULL,
  activity_instance_id INTEGER NULL,
  activity_id TEXT NULL,
  activity_name TEXT NULL,
  name TEXT NULL,
  value TEXT NULL,
  media_type TEXT NULL,
  updated_datetime TEXT NULL
)");

            EnsureSqliteIdentityTable(
                kit,
                "wf_log",
                @"CREATE TABLE wf_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  event_type_id INTEGER NULL,
  priority INTEGER NULL,
  severity TEXT NULL,
  title TEXT NULL,
  message TEXT NULL,
  stack_trace TEXT NULL,
  inner_stack_trace TEXT NULL,
  request_data TEXT NULL,
  time_stamp TEXT NULL
)");

            EnsureAssigneeGroupColumns(kit);
            EnsureAssigneeUnitColumns(kit);
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
                // 非 SQLite 或缺表时忽略；createTable / EnsureSqliteIdentityTable 会建齐
            }
        }

        static void EnsureSqliteIdentityTable(SQLBuilder kit, string tableName, string createSql)
        {
            var needRebuild = true;
            try
            {
                // 已是 rowid 自增主键则跳过
                var sql = kit.exeQuery(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='" + tableName + "'");
                if (sql.Rows.Count > 0)
                {
                    var ddl = Convert.ToString(sql.Rows[0][0]) ?? "";
                    if (ddl.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0
                        && ddl.IndexOf("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase) >= 0)
                        needRebuild = false;
                }
            }
            catch
            {
                needRebuild = true;
            }

            if (!needRebuild) return;

            var tmp = tableName + "__new";
            try { kit.exeNonQuery("DROP TABLE IF EXISTS " + tmp); } catch { /* ignore */ }

            // CREATE TABLE xxx__new (...); 把建表语句中的表名替换为临时名
            var createTmp = createSql.Replace(
                "CREATE TABLE " + tableName,
                "CREATE TABLE " + tmp,
                StringComparison.OrdinalIgnoreCase);
            if (ReferenceEquals(createTmp, createSql) || createTmp == createSql)
                createTmp = createSql.Replace("CREATE TABLE [" + tableName + "]", "CREATE TABLE [" + tmp + "]", StringComparison.OrdinalIgnoreCase);
            kit.exeNonQuery(createTmp);

            try
            {
                var exists = kit.exeQuery(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'");
                if (exists.Rows.Count > 0)
                    kit.exeNonQuery("INSERT INTO " + tmp + " SELECT * FROM " + tableName);
            }
            catch
            {
                // 列不兼容时仍以新表为准（开发库可接受）
            }

            try { kit.exeNonQuery("DROP TABLE IF EXISTS " + tableName); } catch { /* ignore */ }
            kit.exeNonQuery("ALTER TABLE " + tmp + " RENAME TO " + tableName);
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
