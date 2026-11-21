using System;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>Sync Hook 未捕获业务/运行异常（与 <see cref="FlowSyncHookTimeoutException"/> 并列，便于 API 映射）。</summary>
    public sealed class FlowSyncHookException : Exception
    {
        public FlowSyncHookException(
            string hookName,
            EventFireTypeEnum eventType,
            Exception inner)
            : base(
                $"Sync workflow hook '{hookName}' on {eventType} failed: {inner?.Message}",
                inner)
        {
            HookName = hookName;
            EventType = eventType;
        }

        public string HookName { get; }
        public EventFireTypeEnum EventType { get; }
    }
}
