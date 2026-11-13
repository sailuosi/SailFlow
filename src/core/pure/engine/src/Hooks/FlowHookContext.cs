using mooSQL.data;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>Gate / Hook 统一上下文（Gate 默认只读）。</summary>
    public sealed class FlowHookContext
    {
        public string AppName { get; set; }
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        public string TaskId { get; set; }
        public string AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public FlowOperation Operation { get; set; }
        public EventFireTypeEnum EventType { get; set; }

        /// <summary>
        /// 当前流转事务上的 <see cref="SQLBuilder"/>（Sync Hook 同事务读写未提交行时必须用此 kit，
        /// 勿再 <c>Sail.UseRunSQL()</c> 开新连接，否则会与主事务锁死）。
        /// AsyncOutbox 回放时可能为空。
        /// </summary>
        public SQLBuilder Kit { get; set; }
    }

    /// <summary>待办 Notifier 上下文。</summary>
    public sealed class TaskNotifyContext
    {
        public string AppName { get; set; }
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityInstanceId { get; set; }
        public string TaskId { get; set; }
        public string AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public TaskNotifyKind Kind { get; set; }
    }
}
