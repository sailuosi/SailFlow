using sailflow.data;
using sailflow.run;

namespace sailflow.lanes
{
    /// <summary>
    /// Transition 族 Core 上下文：持本命令 Runtime 编排袋（可含 Forward/回退路径）。
    /// Assignee / Lifecycle 族不得持有 <see cref="Runtime"/>。
    /// </summary>
    public sealed class TransitionLaneContext
    {
        public string ProcessInstanceId { get; init; }
        public ActivityInstanceEntity RunningActivityInstance { get; init; }
        public string TaskId { get; init; }

        /// <summary>本命令 Runtime（<see cref="IWfRuntime"/>；仅 Transition 族可见用法）。</summary>
        internal IWfRuntime Runtime { get; init; }
    }
}
