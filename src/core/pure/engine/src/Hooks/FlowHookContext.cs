using System.Text.Json.Serialization;
using mooSQL.data;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>Gate / Hook 统一上下文（Gate 默认只读）。</summary>
    public sealed class FlowHookContext
    {
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        /// <summary>当前节点定义 BizState。</summary>
        public int ActivityBizState { get; set; }
        /// <summary>当前节点定义 BizCode。</summary>
        public string ActivityBizCode { get; set; }
        public string TaskId { get; set; }
        public string SignId { get; set; }
        public string SignName { get; set; }
        public FlowOperation Operation { get; set; }
        public EventFireTypeEnum EventType { get; set; }

        /// <summary>
        /// 当前流转事务上的 <see cref="SQLBuilder"/>（Sync Hook 同事务读写未提交行时必须用此 kit，
        /// 勿再 <c>Sail.UseRunSQL()</c> 开新连接，否则会与主事务锁死）。
        /// AsyncOutbox 回放时为空；禁止序列化进 Outbox（SQLBuilder 自引用环）。
        /// Enqueue 前会临时摘掉；STJ 亦忽略本属性。
        /// </summary>
        [JsonIgnore]
        public SQLBuilder Kit { get; set; }
    }

    /// <summary>待办 Notifier 上下文。</summary>
    public sealed class TaskNotifyContext
    {
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        /// <summary>当前节点定义 BizState。</summary>
        public int ActivityBizState { get; set; }
        /// <summary>当前节点定义 BizCode。</summary>
        public string ActivityBizCode { get; set; }
        public string TaskId { get; set; }
        public string SignId { get; set; }
        public string SignName { get; set; }
        public TaskNotifyKind Kind { get; set; }
    }

    /// <summary>抄送 Notifier 上下文（产品抄送箱；禁止与待办 TaskNotify 混用）。</summary>
    public sealed class CopytoNotifyContext
    {
        public string ProcessCopytoId { get; set; }
        public string BizChannel { get; set; }
        public string BizPK { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string ProcessName { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        /// <summary>被抄送人。</summary>
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string StartUserId { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }
        public string Reason { get; set; }
        public CopytoNotifyKind Kind { get; set; }
    }
}
