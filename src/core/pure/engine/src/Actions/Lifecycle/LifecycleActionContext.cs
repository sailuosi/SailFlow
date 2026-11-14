using sailflow.engine.Entity;

namespace sailflow.engine.Actions.Lifecycle
{
    /// <summary>
    /// Lifecycle 族 Core 上下文：以 ProcessInstance 为主；Task/Activity 可选。
    /// 故意不含图推进（Forward / Runtime）能力。
    /// </summary>
    public sealed class LifecycleActionContext
    {
        public required string ProcessInstanceId { get; init; }
        public ProcessInstanceEntity ProcessInstance { get; init; }
        public string TaskId { get; init; }
        public ActivityInstanceEntity ActivityInstance { get; init; }
    }
}
