using System;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>Hook / Notifier 异步 Outbox（Phase 1 同库落表）。</summary>
    [SooTable(Name = "wf_hook_outbox", Caption = "钩子Outbox", Type = DBTableType.Table)]
    public class HookOutboxEntity
    {
        [SooColumn(Name = "ho_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string HookOutboxId { get; set; }

        [SooColumn(Name = "ho_kind", Length = 64)]
        public string Kind { get; set; }

        [SooColumn(Name = "ho_handler_name", Length = 128)]
        public string HandlerName { get; set; }

        [SooColumn(Name = "ho_evt_type", Length = 128)]
        public string EventType { get; set; }

        [SooColumn(Name = "ho_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "ho_proc_code", Length = 64)]
        public string ProcessCode { get; set; }

        [SooColumn(Name = "ho_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Name = "ho_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        [SooColumn(Name = "ho_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string TaskId { get; set; }

        [SooColumn(Name = "ho_payload_json", DataType = DataFam.Json, Tag = "sail.xml")]
        public string PayloadJson { get; set; }

        [SooColumn(Name = "ho_status", Length = 64)]
        public string Status { get; set; }

        [SooColumn(Name = "ho_attempt")]
        public int? Attempt { get; set; }

        [SooColumn(Name = "ho_nxt_retry_at")]
        public DateTime? NextRetryAt { get; set; }

        [SooColumn(Name = "ho_last_err", DataType = DataFam.Json, Tag = "sail.xml")]
        public string LastError { get; set; }

        [SooColumn(Name = "ho_crt_time")]
        public DateTime? CreatedAt { get; set; }

        [SooColumn(Name = "ho_upd_time")]
        public DateTime? UpdatedAt { get; set; }

        [SooColumn(Name = "ho_lease_until")]
        public DateTime? LeasedUntil { get; set; }
    }

    public static class HookOutboxKinds
    {
        public const string Hook = "Hook";
        public const string Notify = "Notify";

        /// <summary>R10：待办人自动审批锚点（Assignee Filter/EmptyAction 入队；普通 Job 消费）。</summary>
        public const string AssigneeAutoApprove = "AssigneeAutoApprove";
    }

    public static class HookOutboxStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
        public const string Dead = "Dead";

        /// <summary>策略性终态跳过（不代办、不进 Failed 重试梯）。</summary>
        public const string Skipped = "Skipped";
    }
}