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
    /// <para>
    /// 主键为 string + AutoKey=Guid（非库自增）。已有 INTEGER 自增库无法靠 upsert 改类型，开发环境请删库重建。
    /// </para>
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
            // upsert：缺表建表，已有表补齐实体新增列（替代手工 Ensure*Column）
            wk.upsertMode()
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

            HotPathIndexEnsurer.Ensure(kit);
            EnsureTaskDetailsView(kit);
            return wk.WorkingProgress?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 待办/已办依赖的任务明细视图（任务 ⊕ 活动实例 ⊕ 流程实例）。
        /// Clip 按 View 名查询；库中无此视图时会报 no such table。
        /// 每次 drop+create，保证增量列变更后视图定义同步。
        /// </summary>
        public static void EnsureTaskDetailsView(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            const string viewName = "vw_wf_task_details";
            var ddl = kit.useDDL();
            try
            {
                if (ddl.hasView(viewName))
                    ddl.doDropView(viewName);
            }
            catch
            {
                // 方言/缺视图时忽略
            }

            ddl.clear()
                .createViewFromClip(BuildTaskDetailsViewSelect, viewName)
                .doCreateView();
        }

        /// <summary>
        /// 用 SQLClip 定义 <c>vw_wf_task_details</c> 的 SELECT（JOIN 与列来自实体映射）。
        /// <paramref name="mai"/> 的 activity_state 须显式 AS <c>main_activity_state</c>，
        /// 以匹配 <see cref="TaskViewEntity"/> 列名（Clip 匿名投影不会把 rename 落到 SQL AS）。
        /// </summary>
        static void BuildTaskDetailsViewSelect(SQLClip clip)
        {
            clip.from<TaskEntity>(out var t);
            clip.InnerJoin<ActivityInstanceEntity>(out var ai)
                .on(() => t.ActivityInstanceId == ai.Id);
            clip.InnerJoin<ProcessInstanceEntity>(out var pi)
                .on(() => t.ProcessInstanceId == pi.Id);
            clip.LeftJoin<ActivityInstanceEntity>(out var mai)
                .on(() => ai.MainActivityInstanceId == mai.Id);

            clip.select(() => new
            {
                t.Id,
                t.AppName,
                t.AppInstanceId,
                ai.AppInstanceCode,
                t.ProcessInstanceId,
                t.ProcessId,
                pi.Version,
                t.ActivityId,
                t.ActivityInstanceId,
                t.ActivityName,
                ai.ActivityCode,
                ai.ActivityType,
                ai.WorkItemType,
                t.TaskType,
                pi.SubProcessType,
                pi.SubProcessDefId,
                pi.SubProcessId,
                t.EntrustedTaskId,
                ai.MainActivityInstanceId,
                ai.CompleteOrder,
                t.AssignedUserId,
                t.AssignedUserName,
                t.GroupId,
                t.GroupName,
                t.GroupRole,
                t.UnitId,
                t.UnitCode,
                t.UnitName,
                t.IsEMailSent,
                ai.ApprovalStatus,
                t.CreatedDateTime,
                t.ActivatedDateTime,
                t.OverdueDateTime,
                t.UpdatedDateTime,
                t.EndedDateTime,
                t.EndedUserId,
                t.EndedUserName,
                t.TaskState,
                ai.ActivityState,
                t.RecordStatusInvalid,
                pi.ProcessState
            });
            // out var mai → 表别名 mai；须与 TaskViewEntity.MainActivityState 的 SooColumn 一致
            clip.select("mai.activity_state AS main_activity_state");
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
            // 可发布的最小 BPMN：Start → UserTask(R6) → End（sf 命名空间须为 saiflow）
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<bpmn:definitions xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\" " +
                "xmlns:bpmndi=\"http://www.omg.org/spec/BPMN/20100524/DI\" " +
                "xmlns:dc=\"http://www.omg.org/spec/DD/20100524/DC\" " +
                "xmlns:di=\"http://www.omg.org/spec/DD/20100524/DI\" " +
                "xmlns:sf=\"http://www.saiflow.com/schema/sf\" " +
                "id=\"Definitions_SailSeed\" targetNamespace=\"http://bpmn.io/schema/bpmn\">" +
                "<bpmn:process id=\"Process_1\" name=\"Sail Seed Hello\" isExecutable=\"true\" " +
                "sf:code=\"SEED_HELLO\" sf:version=\"1\">" +
                "<bpmn:startEvent id=\"StartEvent_1\" name=\"Start\">" +
                "<bpmn:outgoing>Flow_start_task</bpmn:outgoing>" +
                "</bpmn:startEvent>" +
                "<bpmn:userTask id=\"UserTask_1\" name=\"Hello Task\">" +
                "<bpmn:incoming>Flow_start_task</bpmn:incoming>" +
                "<bpmn:outgoing>Flow_task_end</bpmn:outgoing>" +
                "<bpmn:extensionElements>" +
                "<sf:assigneeResolve type=\"ApproverPick\" params=\"\" />" +
                "<sf:leaveJudge type=\"Collaborate\" params=\"{&quot;completeRate&quot;:1}\" />" +
                "</bpmn:extensionElements>" +
                "</bpmn:userTask>" +
                "<bpmn:endEvent id=\"EndEvent_1\" name=\"End\">" +
                "<bpmn:incoming>Flow_task_end</bpmn:incoming>" +
                "</bpmn:endEvent>" +
                "<bpmn:sequenceFlow id=\"Flow_start_task\" sourceRef=\"StartEvent_1\" targetRef=\"UserTask_1\" />" +
                "<bpmn:sequenceFlow id=\"Flow_task_end\" sourceRef=\"UserTask_1\" targetRef=\"EndEvent_1\" />" +
                "</bpmn:process>" +
                "<bpmndi:BPMNDiagram id=\"BPMNDiagram_1\">" +
                "<bpmndi:BPMNPlane id=\"BPMNPlane_1\" bpmnElement=\"Process_1\">" +
                "<bpmndi:BPMNShape id=\"StartEvent_1_di\" bpmnElement=\"StartEvent_1\">" +
                "<dc:Bounds x=\"180\" y=\"160\" width=\"36\" height=\"36\" />" +
                "</bpmndi:BPMNShape>" +
                "<bpmndi:BPMNShape id=\"UserTask_1_di\" bpmnElement=\"UserTask_1\">" +
                "<dc:Bounds x=\"280\" y=\"138\" width=\"100\" height=\"80\" />" +
                "</bpmndi:BPMNShape>" +
                "<bpmndi:BPMNShape id=\"EndEvent_1_di\" bpmnElement=\"EndEvent_1\">" +
                "<dc:Bounds x=\"450\" y=\"160\" width=\"36\" height=\"36\" />" +
                "</bpmndi:BPMNShape>" +
                "<bpmndi:BPMNEdge id=\"Flow_start_task_di\" bpmnElement=\"Flow_start_task\">" +
                "<di:waypoint x=\"216\" y=\"178\" /><di:waypoint x=\"280\" y=\"178\" />" +
                "</bpmndi:BPMNEdge>" +
                "<bpmndi:BPMNEdge id=\"Flow_task_end_di\" bpmnElement=\"Flow_task_end\">" +
                "<di:waypoint x=\"380\" y=\"178\" /><di:waypoint x=\"450\" y=\"178\" />" +
                "</bpmndi:BPMNEdge>" +
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
