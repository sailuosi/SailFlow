namespace sailflow.engine.Hooks
{
    /// <summary>待办推送开/关消息意图（渠道模板辅助）。</summary>
    public enum TaskNotifyMessageIntent
    {
        Open,
        Close,
    }

    public static class TaskNotifyMessageIntentMapping
    {
        public static TaskNotifyMessageIntent FromKind(TaskNotifyKind kind) =>
            kind switch
            {
                TaskNotifyKind.Created or TaskNotifyKind.Entrusted => TaskNotifyMessageIntent.Open,
                TaskNotifyKind.Completed or TaskNotifyKind.Cancelled => TaskNotifyMessageIntent.Close,
                _ => TaskNotifyMessageIntent.Open,
            };

        public static bool IsOpen(TaskNotifyKind kind) =>
            FromKind(kind) == TaskNotifyMessageIntent.Open;
    }
}
