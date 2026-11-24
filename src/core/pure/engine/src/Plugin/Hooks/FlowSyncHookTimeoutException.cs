using System;
using sailflow.events;

namespace sailflow.hooks
{
    /// <summary>
    /// Sync Hook 软超时：<c>Handle</c> 返回后耗时超过钩子 <c>TimeoutMs</c>（或全局回退）。
    /// 不保证打断阻塞中的 SQL；永久阻塞靠 BEGIN 无 END 归因。
    /// </summary>
    public sealed class FlowSyncHookTimeoutException : Exception
    {
        public FlowSyncHookTimeoutException(string hookName, int timeoutMs)
            : this(hookName, default, timeoutMs, timeoutMs, kitPresent: false)
        {
        }

        public FlowSyncHookTimeoutException(
            string hookName,
            EventFireTypeEnum eventType,
            int timeoutMs,
            long elapsedMs,
            bool kitPresent)
            : base(
                $"Sync workflow hook '{hookName}' on {eventType} exceeded {timeoutMs} ms " +
                $"(elapsed={elapsedMs}ms, kit={kitPresent}). " +
                "Check sync-hook BEGIN without END in logs; prefer FlowHookContext.Kit for engine DB access.")
        {
            HookName = hookName;
            EventType = eventType;
            TimeoutMs = timeoutMs;
            ElapsedMs = elapsedMs;
            KitPresent = kitPresent;
        }

        public string HookName { get; }
        public EventFireTypeEnum EventType { get; }
        public int TimeoutMs { get; }
        public long ElapsedMs { get; }
        public bool KitPresent { get; }
    }
}
