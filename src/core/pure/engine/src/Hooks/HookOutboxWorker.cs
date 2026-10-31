using mooSQL.data;
using saiflow.engine.Client;

namespace saiflow.engine.Hooks
{
    /// <summary>Phase 1 最小 Outbox 消费（Phase 2 完善退避与租约）。</summary>
    public sealed class HookOutboxWorker
    {
        public HookOutboxWorker(SailClient client)
        {
            Sail = client;
            Dispatcher = new WorkflowCapabilityDispatcher(client);
        }

        public SailClient Sail { get; }
        public WorkflowCapabilityDispatcher Dispatcher { get; }

        /// <summary>处理一批 Pending 记录。</summary>
        public int ProcessPending(int limit = 50)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            try
            {
                var n = Dispatcher.DispatchPendingOutbox(kit, limit);
                kit.commit();
                return n;
            }
            catch
            {
                throw;
            }
        }

        public int ProcessPending(SQLBuilder kit, int limit = 50) =>
            Dispatcher.DispatchPendingOutbox(kit, limit);
    }
}
