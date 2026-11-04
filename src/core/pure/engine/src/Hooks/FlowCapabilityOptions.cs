namespace saiflow.engine.Hooks
{
    /// <summary>Gate / Hook 运行时行为（Client 级）。</summary>
    public sealed class FlowCapabilityOptions
    {
        /// <summary>
        /// Sync Hook 硬超时（毫秒）。默认 2000；0 表示不限制。
        /// 超时抛出 <see cref="FlowSyncHookTimeoutException"/>，FailFast 回滚主事务。
        /// </summary>
        public int SyncHookTimeoutMs { get; set; } = 2000;
    }
}
