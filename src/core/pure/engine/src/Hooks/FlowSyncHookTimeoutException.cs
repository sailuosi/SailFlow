using System;

namespace sailflow.engine.Hooks
{
    /// <summary>Sync Hook 在 <see cref="FlowCapabilityOptions.SyncHookTimeoutMs"/> 内未完成。</summary>
    public sealed class FlowSyncHookTimeoutException : Exception
    {
        public FlowSyncHookTimeoutException(string hookName, int timeoutMs)
            : base($"Sync workflow hook '{hookName}' timed out after {timeoutMs} ms.")
        {
            HookName = hookName;
            TimeoutMs = timeoutMs;
        }

        public string HookName { get; }
        public int TimeoutMs { get; }
    }
}
