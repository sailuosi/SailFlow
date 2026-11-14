using sailflow.engine.Entity;

namespace sailflow.engine.Actions.Assignee
{
    /// <summary>
    /// Assignee 族 Core 上下文：Task / ActivityInstance / ProcessInstanceId。
    /// 故意不含图推进（Forward / Runtime）能力。
    /// </summary>
    public sealed class AssigneeActionContext
    {
        public required TaskViewEntity TaskView { get; init; }
        public required ActivityInstanceEntity ActivityInstance { get; init; }
        public required string ProcessInstanceId { get; init; }
        public required string TaskId { get; init; }
    }
}
