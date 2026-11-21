namespace sailflow.engine.Logging
{
    /// <summary>运行时日志 category 约定（便于过滤）。</summary>
    public static class SailLogCategories
    {
        public const string SyncHook = "sync-hook";
        public const string AsyncHook = "async-hook";
        public const string Gate = "gate";
        public const string Notifier = "notifier";
        public const string CreateHook = "create-hook";
        public const string Assignee = "assignee";
        public const string Outbox = "outbox";

        /// <summary>流转族动作（Start/Run/Reject…）。</summary>
        public const string Transition = "transition";

        /// <summary>办理人变化族（Transfer/Delegate…）。</summary>
        public const string AssigneeAction = "assignee-action";

        /// <summary>实例生命周期族（Cancel/Suspend…）。</summary>
        public const string Lifecycle = "lifecycle";

        /// <summary>沟通等待等硬锁。</summary>
        public const string Guard = "guard";

        /// <summary>Manager / Runtime 等通用引擎失败。</summary>
        public const string Engine = "engine";
    }
}
