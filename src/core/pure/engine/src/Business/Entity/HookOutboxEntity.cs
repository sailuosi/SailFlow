using System;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>Hook / Notifier 异步 Outbox（Phase 1 同库落表）。</summary>
    [SooTable(Name = "wf_hook_outbox", Caption = "钩子Outbox", Type = DBTableType.Table)]
    public class HookOutboxEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string HookOutboxId { get; set; }

        [SooColumn(Name = "kind", Length = 64)]
        public string Kind { get; set; }

        [SooColumn(Name = "handler_name", Length = 128)]
        public string HandlerName { get; set; }

        [SooColumn(Name = "event_type", Length = 128)]
        public string EventType { get; set; }

        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "process_code", Length = 64)]
        public string ProcessCode { get; set; }

        [SooColumn(Name = "process_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Name = "activity_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        [SooColumn(Name = "task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string TaskId { get; set; }

        [SooColumn(Name = "payload_json", DataType = DataFam.Json, Tag = "sail.xml")]
        public string PayloadJson { get; set; }

        [SooColumn(Name = "status", Length = 64)]
        public string Status { get; set; }

        [SooColumn(Name = "attempt")]
        public int? Attempt { get; set; }

        [SooColumn(Name = "next_retry_at")]
        public DateTime? NextRetryAt { get; set; }

        [SooColumn(Name = "last_error", DataType = DataFam.Json, Tag = "sail.xml")]
        public string LastError { get; set; }

        [SooColumn(Name = "created_at")]
        public DateTime? CreatedAt { get; set; }

        [SooColumn(Name = "updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [SooColumn(Name = "leased_until")]
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
