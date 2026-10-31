using System;

namespace saiflow.engine.Hooks
{
    /// <summary>Outbox Worker 运行参数（Phase 2+3）。</summary>
    public sealed class HookOutboxOptions
    {
        public const int DefaultMaxAttempts = 5;
        public const int DefaultBatchSize = 50;
        public const int DefaultProcessingStaleSeconds = 300;
        public const int DefaultLeaseSeconds = 120;
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

        public int MaxAttempts { get; set; } = DefaultMaxAttempts;
        public int BatchSize { get; set; } = DefaultBatchSize;

        /// <summary>Processing 行无有效 lease 时，UpdatedAt 早于此阈值则回收为 Pending。</summary>
        public int ProcessingStaleSeconds { get; set; } = DefaultProcessingStaleSeconds;

        /// <summary>认领 Pending 时写入 leased_until = now + LeaseSeconds。</summary>
        public int LeaseSeconds { get; set; } = DefaultLeaseSeconds;

        /// <summary>Drain 循环间隔。</summary>
        public TimeSpan PollInterval { get; set; } = DefaultPollInterval;
    }
}
