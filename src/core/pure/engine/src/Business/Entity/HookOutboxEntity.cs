using System;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// Hook / Notifier 异步 Outbox（表 <c>sf_hook_outbox</c>；Phase 1 同库落表）。
    /// 流转成功后与主事务同库写入 Pending；后台 Drain 认领 → 调 Handler → Succeeded / 重试 / Dead。
    /// </summary>
    [SooTable(Name = "sf_hook_outbox", Caption = "钩子Outbox", Type = DBTableType.Table)]
    public class HookOutboxEntity
    {
        /// <summary>
        /// Outbox 行主键（列 <c>ho_id</c>）。
        /// </summary>
        [SooColumn(Name = "ho_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string HookOutboxId { get; set; }

        /// <summary>
        /// 消息种类（列 <c>ho_kind</c>），见 <see cref="HookOutboxKinds"/>。
        /// Hook / Notify / CopytoNotify / AssigneeAutoApprove 等。
        /// </summary>
        [SooColumn(Name = "ho_kind", Length = 64)]
        public string Kind { get; set; }

        /// <summary>
        /// 处理器类型名（列 <c>ho_handler_name</c>），Drain 时解析并调用对应 Handler。
        /// </summary>
        [SooColumn(Name = "ho_handler_name", Length = 128)]
        public string HandlerName { get; set; }

        /// <summary>
        /// 事件类型名（列 <c>ho_evt_type</c>），如 OnProcessContinued 等 EventFireType。
        /// </summary>
        [SooColumn(Name = "ho_evt_type", Length = 128)]
        public string EventType { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>ho_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "ho_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程编码（列 <c>ho_proc_code</c>），便于按 ProcessCodes 排查。
        /// </summary>
        [SooColumn(Name = "ho_proc_code", Length = 64)]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 流程实例 Id（列 <c>ho_proc_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "ho_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 活动实例 Id（列 <c>ho_act_instance_id</c>）；无活动上下文时可空。
        /// </summary>
        [SooColumn(Name = "ho_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        /// <summary>
        /// 任务 Id（列 <c>ho_task_id</c>）；Notifier / 待办相关必填，纯实例 Hook 可空。
        /// </summary>
        [SooColumn(Name = "ho_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string TaskId { get; set; }

        /// <summary>
        /// 调用载荷 JSON（列 <c>ho_payload_json</c>），反序列化为 Hook/Notify 上下文。
        /// </summary>
        [SooColumn(Name = "ho_payload_json", DataType = DataFam.Json, Tag = "sail.xml")]
        public string PayloadJson { get; set; }

        /// <summary>
        /// 投递状态（列 <c>ho_status</c>），见 <see cref="HookOutboxStatus"/>。
        /// Pending → Processing → Succeeded；失败进 Failed 重试，超限 Dead；Skipped 为策略性终态。
        /// </summary>
        [SooColumn(Name = "ho_status", Length = 64)]
        public string Status { get; set; }

        /// <summary>
        /// 已尝试次数（列 <c>ho_attempt</c>），Drain 每次消费递增。
        /// </summary>
        [SooColumn(Name = "ho_attempt")]
        public int? Attempt { get; set; }

        /// <summary>
        /// 下次可重试时间（列 <c>ho_nxt_retry_at</c>）；未到点不被认领。
        /// </summary>
        [SooColumn(Name = "ho_nxt_retry_at")]
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// 最近一次错误信息（列 <c>ho_last_err</c>）。
        /// </summary>
        [SooColumn(Name = "ho_last_err", DataType = DataFam.Json, Tag = "sail.xml")]
        public string LastError { get; set; }

        /// <summary>
        /// 入队时间（列 <c>ho_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "ho_crt_time")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 最后状态变更时间（列 <c>ho_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "ho_upd_time")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 租约到期时间（列 <c>ho_lease_until</c>）。
        /// Drain 认领时写入，防止多消费者重复处理；过期可被再次认领。
        /// </summary>
        [SooColumn(Name = "ho_lease_until")]
        public DateTime? LeasedUntil { get; set; }
    }

    /// <summary>Outbox Kind 常量。</summary>
    public static class HookOutboxKinds
    {
        /// <summary>异步 Hook 回调。</summary>
        public const string Hook = "Hook";

        /// <summary>待办 Notifier 推送。</summary>
        public const string Notify = "Notify";

        /// <summary>抄送旁路通知（独立于待办 Notify）。</summary>
        public const string CopytoNotify = "CopytoNotify";

        /// <summary>R10：待办人自动审批锚点（Assignee Filter/EmptyAction 入队；普通 Job 消费）。</summary>
        public const string AssigneeAutoApprove = "AssigneeAutoApprove";
    }

    /// <summary>Outbox Status 常量。</summary>
    public static class HookOutboxStatus
    {
        /// <summary>待认领。</summary>
        public const string Pending = "Pending";

        /// <summary>处理中（已持租约）。</summary>
        public const string Processing = "Processing";

        /// <summary>成功终态。</summary>
        public const string Succeeded = "Succeeded";

        /// <summary>失败，可按 NextRetryAt 重试。</summary>
        public const string Failed = "Failed";

        /// <summary>死信；需 ResetDead* 复活。</summary>
        public const string Dead = "Dead";

        /// <summary>策略性终态跳过（不代办、不进 Failed 重试梯）。</summary>
        public const string Skipped = "Skipped";
    }
}
