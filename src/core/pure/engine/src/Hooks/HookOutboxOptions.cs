using System;

namespace sailflow.engine.Hooks
{
    /// <summary>Outbox Worker 运行参数（Phase 2+3）+ TTL 清理。</summary>
    public sealed class HookOutboxOptions
    {
        public const int DefaultMaxAttempts = 5;
        public const int DefaultBatchSize = 50;
        public const int DefaultProcessingStaleSeconds = 300;
        public const int DefaultLeaseSeconds = 120;
        public const int DefaultSucceededRetentionDays = 7;
        public const int DefaultDeadRetentionDays = 30;
        public const int DefaultPurgeBatchSize = 200;
        public const int DefaultPurgeEveryDrainCycles = 12;
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

        public int MaxAttempts { get; set; } = DefaultMaxAttempts;
        public int BatchSize { get; set; } = DefaultBatchSize;

        /// <summary>Processing 行无有效 lease 时，UpdatedAt 早于此秒数则回收为 Pending。</summary>
        public int ProcessingStaleSeconds { get; set; } = DefaultProcessingStaleSeconds;

        /// <summary>认领 Pending 时写入 leased_until = now + LeaseSeconds。</summary>
        public int LeaseSeconds { get; set; } = DefaultLeaseSeconds;

        /// <summary>Drain 循环间隔。</summary>
        public TimeSpan PollInterval { get; set; } = DefaultPollInterval;

        /// <summary>Succeeded 行保留天数（按 UpdatedAt）；≤0 表示不自动清理 Succeeded。</summary>
        public int SucceededRetentionDays { get; set; } = DefaultSucceededRetentionDays;

        /// <summary>Dead 行保留天数（按 UpdatedAt）；≤0 表示不自动清理 Dead。</summary>
        public int DeadRetentionDays { get; set; } = DefaultDeadRetentionDays;

        /// <summary>单次 TTL 清理最多删除行数。</summary>
        public int PurgeBatchSize { get; set; } = DefaultPurgeBatchSize;

        /// <summary>Drain 每 N 轮执行一次 TTL 清理。</summary>
        public int PurgeEveryDrainCycles { get; set; } = DefaultPurgeEveryDrainCycles;
    }
}
