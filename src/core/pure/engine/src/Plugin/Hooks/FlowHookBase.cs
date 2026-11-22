namespace sailflow.engine.Hooks
{
    /// <summary>
    /// Sync/Async 钩子基类。默认软超时 30s；子类可改 <see cref="TimeoutMs"/>。
    /// </summary>
    /// <remarks>
    /// 有 <c>ctx.Kit</c> 时与主事务同连接；勿另开 <c>UseRunSQL</c> 读写引擎库。
    /// <c>OnTransactionCommitted</c> / <c>OnTransactionFailed</c> 时 Kit 恒空、BestEffort（超时/异常只记日志）。
    /// 详见 theme《专题-钩子治理与同事务可观测》《专题-事务一致性》。
    /// </remarks>
    public abstract class FlowHookBase : IFlowHook
    {
        public abstract string Name { get; }

        /// <summary>中文显示名（日志用）；子类宜覆盖。</summary>
        public virtual string Title => null;

        /// <summary>
        /// Sync 软超时（毫秒）。默认 30000；子类可 override 或赋值。
        /// ≤0 表示不限制（仍输出 BEGIN/END）。
        /// </summary>
        public virtual int TimeoutMs { get; set; } = 30_000;

        public abstract void Handle(FlowHookContext ctx);
    }
}
