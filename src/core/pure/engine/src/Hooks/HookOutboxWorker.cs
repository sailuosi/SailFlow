using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Client;

namespace saiflow.engine.Hooks
{
    /// <summary>Outbox 消费：认领与逐条短事务隔离（Phase 2）。</summary>
    public sealed class HookOutboxWorker
    {
        public HookOutboxWorker(SailClient client, HookOutboxOptions options = null)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Options = options ?? new HookOutboxOptions();
            Dispatcher = new WorkflowCapabilityDispatcher(client);
        }

        public SailClient Sail { get; }
        public HookOutboxOptions Options { get; }
        public WorkflowCapabilityDispatcher Dispatcher { get; }

        /// <summary>认领一批 Pending，逐条 dispatch + 标记（各自短事务）。</summary>
        public int ProcessPending(int? limit = null, CancellationToken cancellationToken = default) =>
            ProcessPendingAsync(limit, cancellationToken).GetAwaiter().GetResult();

        public async Task<int> ProcessPendingAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            var batchSize = limit ?? Options.BatchSize;
            var om = new HookOutboxManager(Sail);
            IList<HookOutboxEntity> claimed;

            using (var claimKit = Sail.UseRunSQL())
            {
                claimKit.beginTransaction();
                try
                {
                    claimed = om.TryClaimPending(claimKit, batchSize, Options.MaxAttempts);
                    claimKit.commit();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[saiflow] Hook outbox claim failed: {ex.Message}");
                    throw;
                }
            }

            return await Dispatcher.DispatchClaimedOutboxAsync(
                claimed,
                Options.MaxAttempts,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 在调用方事务内认领；逐条处理使用独立连接/事务（不随 kit 一并回滚成功项）。
        /// </summary>
        public int ProcessPending(SQLBuilder kit, int limit = 50) =>
            ProcessPending(kit, limit, Options.MaxAttempts);

        public int ProcessPending(SQLBuilder kit, int limit, int maxAttempts)
        {
            var om = new HookOutboxManager(Sail);
            var claimed = om.TryClaimPending(kit, limit, maxAttempts);
            return Dispatcher.DispatchClaimedOutbox(claimed, maxAttempts);
        }
    }
}
