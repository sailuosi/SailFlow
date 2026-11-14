using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using mooSQL.data;
using mooSQL.data.model;
using sailflow.AI.Entity;
using sailflow.AI.Runtime;
using sailflow.biz.query;
using sailflow.engine.Client;
using sailflow.engine.Entity;
using sailflow.engine.Hosting;
using sailflow.hub.Form;
using sailflow.hub.Logging;

namespace sailflow.samples.Schema;

/// <summary>
/// Demo：业务宿主侧 Schema 初始化参考实现（核心表 + AI + 视图/索引 + 可选种子）。
/// 复制到业务系统并按产品表扩展；不要放回 sailflow 产品包。
/// </summary>
public static class SampleSchemaInitializer
{
    public sealed class InitDbResult
    {
        public string Progress { get; set; } = string.Empty;
        public int SeededRows { get; set; }
    }

    public static InitDbResult InitDb(SailClient client, bool seed = true, bool includeAi = true, bool registerAiExecutor = true)
    {
        ArgumentNullException.ThrowIfNull(client);

        var kit = client.UseRunSQL();
        var progress = EnsureAllTables(kit, includeAi);
        var seeded = seed ? EnsureMinimalSeed(kit) : 0;

        if (includeAi && registerAiExecutor)
            AiRuntime.UseDefaultExecutor(client);

        return new InitDbResult { Progress = progress, SeededRows = seeded };
    }

    public static string EnsureAllTables(SailClient client, bool includeAi = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        return EnsureAllTables(client.UseRunSQL(), includeAi);
    }

    public static string EnsureAllTables(SQLBuilder kit, bool includeAi = true)
    {
        ArgumentNullException.ThrowIfNull(kit);

        var progress = EnsureCoreTables(kit);
        if (includeAi)
            progress += EnsureAiTables(kit);
        return progress;
    }

    /// <summary>宿主启动示例：PostBuild 注册建表（替代已废弃的 UseSchema）。</summary>
    public static ISailCollection UseSampleSchema(
        this ISailCollection services,
        bool seed = true,
        bool includeAi = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = "Sample.Schema",
            Phase = SailFeaturePhase.PostBuild,
            Order = 900,
            DependsOn = new[] { SailFeatureNames.Database },
            PostBuild = (c, _) => InitDb(c, seed, includeAi, registerAiExecutor: false)
        });
    }

    static string EnsureCoreTables(SQLBuilder kit)
    {
        var wk = kit.useDBInit()
            .upsertMode()
            .columnTypeByTag("sail.id", DataFam.Guid)
            .columnTypeByTag("sail.xml", DataFam.Json)
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
            .createTable<FormEntity>()
            .createTable<FieldActivityEditEntity>()
            .createTable<SailProcessCopytoEntity>();

        EnsureHotPathIndexes(kit);
        EnsureTaskDetailsView(kit);
        return wk.WorkingProgress?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 待办/实例热路径索引：DDLBuilder.hasIndex → setIndex → doCreateIndex（无 IF NOT EXISTS）。
    /// </summary>
    public static void EnsureHotPathIndexes(SQLBuilder kit)
    {
        ArgumentNullException.ThrowIfNull(kit);

        TryCreateIndex(kit, "IX_wf_task_pi", "wf_task", "tk_proc_instance_id");
        TryCreateIndex(kit, "IX_wf_task_assignee_state", "wf_task",
            "tk_asgn_user_id", "tk_task_state", "tk_proc_instance_id");
        TryCreateIndex(kit, "IX_wf_task_state_created", "wf_task",
            "tk_task_state", "tk_crt_time");
        TryCreateIndex(kit, "IX_wf_task_state_overdue", "wf_task",
            "tk_task_state", "tk_overdue_time");
        TryCreateIndex(kit, "IX_wf_ai_pi_state", "wf_act_instance",
            "ai_proc_instance_id", "ai_activity_state");
        TryCreateIndex(kit, "IX_wf_pi_state_created", "wf_proc_instance",
            "pi_process_state", "pi_crt_time");
        TryCreateIndex(kit, "IX_wf_pi_creator_state", "wf_proc_instance",
            "pi_crt_user_id", "pi_process_state", "pi_crt_time");
        TryCreateIndex(kit, "IX_wf_pv_pi_name", "wf_proc_variable",
            "pv_proc_instance_id", "pv_name");
        TryCreateIndex(kit, "IX_wf_ti_pi", "wf_tran_instance",
            "ti_proc_instance_id");
        TryCreateIndex(kit, "IX_wf_hook_outbox_status", "wf_hook_outbox",
            "ho_status", "ho_nxt_retry_at");
    }

    static void TryCreateIndex(SQLBuilder kit, string name, string table, params string[] columns)
    {
        try
        {
            var ddl = kit.useDDL();
            if (ddl.hasIndex(name))
                return;

            ddl.clear()
                .setTable(table)
                .setIndex(new List<string>(columns), name)
                .doCreateIndex();
        }
        catch
        {
            // 缺表或权限不足时忽略
        }
    }

    static string EnsureAiTables(SQLBuilder kit)
    {
        var wk = kit.useDBInit()
            .upsertMode()
            .columnTypeByTag("sail.id", DataFam.Guid)
            .columnTypeByTag("sail.xml", DataFam.Json)
            .createTable<AiModelProviderEntity>()
            .createTable<AiActivityConfigEntity>();
        return wk.WorkingProgress?.ToString() ?? string.Empty;
    }

    public static void EnsureTaskDetailsView(SQLBuilder kit)
    {
        ArgumentNullException.ThrowIfNull(kit);

        const string viewName = "vw_wf_task_detail";
        var ddl = kit.useDDL();
        try
        {
            if (ddl.hasView(viewName))
                ddl.doDropView(viewName);
        }
        catch
        {
            // ignore
        }

        ddl.clear()
            .createViewFromClip(BuildTaskDetailsViewSelect, viewName)
            .doCreateView();
    }

    static void BuildTaskDetailsViewSelect(SQLClip clip)
    {
        clip.from<TaskEntity>(out var t)
            .InnerJoin<ActivityInstanceEntity>(out var ai)
            .on(() => t.ActivityInstanceId == ai.ActivityInstanceId)
            .InnerJoin<ProcessInstanceEntity>(out var pi)
            .on(() => t.ProcessInstanceId == pi.ProcessInstanceId)
            .LeftJoin<ActivityInstanceEntity>(out var mai)
            .on(() => ai.MainActivityInstanceId == mai.ActivityInstanceId);

        // 投影列名须与 TaskViewEntity SooColumn 一致（带表前缀后不可再混用 t./ai./pi. 同名列）。
        clip.select(() => new
        {
            t.TaskId,
            t.BizChannel,
            t.BizPK,
            pi.BizCode,
            pi.Title,
            pi.ProcessName,
            t.ProcessInstanceId,
            pi.ProcessId,
            pi.Version,
            t.ActivityId,
            t.ActivityInstanceId,
            ai.ActivityName,
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
        clip.select("mai.ai_activity_state AS ai_mn_activity_state");
    }

    public static int EnsureMinimalSeed(SQLBuilder kit)
    {
        ArgumentNullException.ThrowIfNull(kit);
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

    static string MinimalBpmnXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<bpmn:definitions xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
        "xmlns:bpmn=\"http://www.omg.org/spec/BPMN/20100524/MODEL\" " +
        "xmlns:bpmndi=\"http://www.omg.org/spec/BPMN/20100524/DI\" " +
        "xmlns:dc=\"http://www.omg.org/spec/DD/20100524/DC\" " +
        "xmlns:di=\"http://www.omg.org/spec/DD/20100524/DI\" " +
        "xmlns:sf=\"http://www.sailflow.com/schema/sf\" " +
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
