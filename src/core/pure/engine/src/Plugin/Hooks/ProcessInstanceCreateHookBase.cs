namespace sailflow.hooks
{
    /// <summary>
    /// 实例创建钩子基类。默认软超时 30s；子类可改 <see cref="TimeoutMs"/>。
    /// </summary>
    public abstract class ProcessInstanceCreateHookBase : IProcessInstanceCreateHook
    {
        public abstract string Name { get; }

        /// <summary>中文显示名（日志用）；子类宜覆盖。</summary>
        public virtual string Title => null;

        /// <summary>
        /// 软超时（毫秒）。默认 30000；≤0 表示不限制（仍输出 BEGIN/END）。
        /// </summary>
        public virtual int TimeoutMs { get; set; } = 30_000;

        public abstract void OnCreating(ProcessInstanceCreateContext ctx);
    }
}
