namespace sailflow.hooks
{
    /// <summary>Gate / Hook 运行时行为（Client 级）。</summary>
    public sealed class FlowCapabilityOptions
    {
        /// <summary>
        /// Sync Hook 硬超时回退（毫秒）。默认 30000，与 <see cref="FlowHookBase.TimeoutMs"/> 对齐。
        /// 仅当钩子未继承基类且注册未指定 <c>TimeoutMs</c> 时使用；0 表示不限制。
        /// 超时抛出 <see cref="FlowSyncHookTimeoutException"/>，FailFast 回滚主事务。
        /// </summary>
        public int SyncHookTimeoutMs { get; set; } = 30_000;

        /// <summary>
        /// 慢钩子 Warning 阈值（毫秒）。默认 500；耗时超过本值且未达硬超时打 SLOW。
        /// ≤0 表示不打 SLOW。
        /// </summary>
        public int SyncHookWarnMs { get; set; } = 500;
    }
}
