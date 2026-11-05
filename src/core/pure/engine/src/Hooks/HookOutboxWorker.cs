using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Client;

namespace saiflow.engine.Hooks
{
    /// <summary>Outbox 消费：认领与逐条短事务隔离（Phase 2）。</summary>
    public sealed class HookOutboxWorker
    {
        public HookOutboxWorker(SailClient client, HookOutboxOptions options = null)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Options = options ?? client.HookOutboxOptions ?? new HookOutboxOptions();
            Dispatcher = new FlowCapabilityDispatcher(client);
        }

        public SailClient Sail { get; }
        public HookOutboxOptions Options { get; }
        public FlowCapabilityDispatcher Dispatcher { get; }

        /// <summary>认领一批 Pending，逐条 dispatch + 标记（各自短事务）。</summary>
        public int ProcessPending(int? limit = null, CancellationToken cancellationToken = default) =>
            ProcessPendingAsync(limit, cancellationToken).GetAwaiter().GetResult();

        public async Task<int> ProcessPendingAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batchSize = limit ?? Options.BatchSize;
            var om = new HookOutboxManager(Sail);
            IList<HookOutboxEntity> claimed;

            using (var claimKit = Sail.UseRunSQL())
            {
                claimKit.beginTransaction();
                try
                {
                    var now = DateTime.UtcNow;
                    var staleBefore = now.AddSeconds(-Options.ProcessingStaleSeconds);
                    om.ReclaimStaleProcessing(claimKit, staleBefore, now);
                    claimed = om.TryClaimPending(
                        claimKit,
                        batchSize,
                        Options.MaxAttempts,
                        Options.LeaseSeconds);
                    claimKit.commit();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[saiflow] Hook outbox claim failed: {ex.Message}");
                    throw;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var processed = await Dispatcher.DispatchClaimedOutboxAsync(
                claimed,
                Options.MaxAttempts,
                cancellationToken).ConfigureAwait(false);

            return processed;
        }

        /// <summary>按 Options TTL 清理终态 Outbox（可单独由 Host 定时唤起）。</summary>
        public int PurgeExpiredTerminal(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var n = new HookOutboxManager(Sail).PurgeExpiredTerminal(
                        kit,
                        Options.SucceededRetentionDays,
                        Options.DeadRetentionDays,
                        Options.PurgeBatchSize);
                    kit.commit();
                    return n;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 在调用方事务内认领；逐条处理使用独立连接/事务（不随 kit 一并回滚成功项）。
        /// </summary>
        public int ProcessPending(SQLBuilder kit, int limit = 50) =>
            ProcessPending(kit, limit, Options.MaxAttempts);

        public int ProcessPending(SQLBuilder kit, int limit, int maxAttempts)
        {
            var om = new HookOutboxManager(Sail);
            var now = DateTime.UtcNow;
            om.ReclaimStaleProcessing(kit, now.AddSeconds(-Options.ProcessingStaleSeconds), now);
            var claimed = om.TryClaimPending(kit, limit, maxAttempts, Options.LeaseSeconds);
            return Dispatcher.DispatchClaimedOutbox(claimed, maxAttempts);
        }
    }
}
