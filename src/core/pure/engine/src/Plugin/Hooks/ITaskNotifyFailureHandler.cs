using System;

namespace sailflow.engine.Hooks
{
    /// <summary>可选：待办推送失败时由业务记录或告警（骨架默认仅 Trace）。</summary>
    public interface ITaskNotifyFailureHandler
    {
        void OnNotifyFailed(TaskNotifyContext ctx, Exception ex, string notifierName);
    }
}
