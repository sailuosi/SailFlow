using sailflow.data;

namespace sailflow.engine.Actions.Assignee
{
    /// <summary>
    /// Assignee 族 Core 上下文：Task / ActivityInstance / ProcessInstanceId。
    /// 故意不含图推进（Forward / Runtime）能力。
    /// </summary>
    public sealed class AssigneeActionContext
    {
        public TaskViewEntity TaskView { get; init; }
        public ActivityInstanceEntity ActivityInstance { get; init; }
        public string ProcessInstanceId { get; init; }
        public string TaskId { get; init; }
    }
}
