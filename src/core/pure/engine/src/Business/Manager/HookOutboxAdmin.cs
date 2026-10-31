using System.Collections.Generic;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.engine.Hooks;

namespace saiflow.engine.Business.Manager
{
    /// <summary>Outbox 运维门面（委托 <see cref="HookOutboxManager"/>）。</summary>
    public sealed class HookOutboxAdmin
    {
        private readonly HookOutboxManager _inner;

        public HookOutboxAdmin(SailClient client) =>
            _inner = new HookOutboxManager(client ?? throw new System.ArgumentNullException(nameof(client)));

        public int ResetDeadBatch(SQLBuilder kit, IEnumerable<int> ids) =>
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
    }
}
