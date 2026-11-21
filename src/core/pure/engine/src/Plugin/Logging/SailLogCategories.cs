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
    }
}
