namespace saiflow.engine.Hooks
{
    /// <summary>Outbox Worker 运行参数（Phase 2）。</summary>
    public sealed class HookOutboxOptions
    {
        public const int DefaultMaxAttempts = 5;
        public const int DefaultBatchSize = 50;

        public int MaxAttempts { get; set; } = DefaultMaxAttempts;
        public int BatchSize { get; set; } = DefaultBatchSize;
    }
}
