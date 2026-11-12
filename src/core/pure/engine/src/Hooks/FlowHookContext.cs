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
