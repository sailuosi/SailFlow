using saiflow.engine.Event;

namespace saiflow.engine.Hooks
{
    /// <summary>Gate / Hook 统一上下文（Gate 默认只读）。</summary>
    public sealed class WorkflowHookContext
    {
        public string AppName { get; set; }
        public string AppInstanceId { get; set; }
        public string ProcessId { get; set; }
        public string ProcessCode { get; set; }
        public string Version { get; set; }
        public int ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public int ActivityInstanceId { get; set; }
        public int? TaskId { get; set; }
        public string AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public WorkflowOperation Operation { get; set; }
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
        public int ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityCode { get; set; }
        public int ActivityInstanceId { get; set; }
        public int TaskId { get; set; }
        public string AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public TaskNotifyKind Kind { get; set; }
    }
}
