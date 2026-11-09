using System.Collections.Generic;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Hooks;

namespace saiflow.engine.Manager
{
    /// <summary>Outbox 运维门面（委托 <see cref="HookOutboxManager"/>）。</summary>
    public sealed class HookOutboxAdmin
    {
        private readonly HookOutboxManager _inner;

        public HookOutboxAdmin(SailClient client)
        {
            if (client == null) throw new System.ArgumentNullException(nameof(client));
            _inner = client.ManagerFactory.HookOutbox;
        }

        public int ResetDeadBatch(SQLBuilder kit, IEnumerable<string> ids) =>
            _inner.ResetDeadBatch(kit, ids);

        public int ResetAllDead(SQLBuilder kit, int limit = 500) =>
            _inner.ResetAllDead(kit, limit);

        public IList<HookOutboxEntity> QueryDead(
            SQLBuilder kit,
            int limit,
            string processCode = null,
            string handlerName = null) =>
            _inner.QueryDead(kit, limit, processCode, handlerName);

        public int CountByStatus(SQLBuilder kit, string status) =>
            _inner.CountByStatus(kit, status);

        /// <summary>按 <see cref="HookOutboxOptions"/> 保留窗清理 Succeeded/Dead（供 Host 定时或 Drain 调用）。</summary>
        public int PurgeExpired(SQLBuilder kit, HookOutboxOptions options = null)
        {
            options ??= new HookOutboxOptions();
            return _inner.PurgeExpiredTerminal(
                kit,
                options.SucceededRetentionDays,
                options.DeadRetentionDays,
                options.PurgeBatchSize);
        }
    }
}
