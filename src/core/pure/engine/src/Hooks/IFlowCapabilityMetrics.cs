namespace saiflow.engine.Hooks
{
    /// <summary>Gate / Hook / Outbox / Notifier 轻量计数（best-effort，不影响主路径）。</summary>
    public interface IFlowCapabilityMetrics
    {
        void IncrementGateReject();
        void IncrementSyncHook();
        void IncrementOutboxEnqueue();
        void IncrementOutboxSucceeded();
        void IncrementOutboxFailed();
        void IncrementOutboxDead();
        void IncrementNotifierDispatch();
        void IncrementSyncHookTimeout();
    }

    public sealed class NullFlowCapabilityMetrics : IFlowCapabilityMetrics
    {
        public static readonly NullFlowCapabilityMetrics Instance = new();

        public void IncrementGateReject() { }
        public void IncrementSyncHook() { }
        public void IncrementOutboxEnqueue() { }
        public void IncrementOutboxSucceeded() { }
        public void IncrementOutboxFailed() { }
        public void IncrementOutboxDead() { }
        public void IncrementNotifierDispatch() { }
        public void IncrementSyncHookTimeout() { }
    }

    /// <summary>进程内计数，供测试或简易观测。</summary>
    public sealed class InMemoryFlowCapabilityMetrics : IFlowCapabilityMetrics
    {
        private long _gateReject;
        private long _syncHook;
        private long _outboxEnqueue;
        private long _outboxSucceeded;
        private long _outboxFailed;
        private long _outboxDead;
        private long _notifierDispatch;
        private long _syncHookTimeout;

        public long GateReject => _gateReject;
        public long SyncHook => _syncHook;
        public long OutboxEnqueue => _outboxEnqueue;
        public long OutboxSucceeded => _outboxSucceeded;
        public long OutboxFailed => _outboxFailed;
        public long OutboxDead => _outboxDead;
        public long NotifierDispatch => _notifierDispatch;
        public long SyncHookTimeout => _syncHookTimeout;

        public void IncrementGateReject() => System.Threading.Interlocked.Increment(ref _gateReject);
        public void IncrementSyncHook() => System.Threading.Interlocked.Increment(ref _syncHook);
        public void IncrementOutboxEnqueue() => System.Threading.Interlocked.Increment(ref _outboxEnqueue);
        public void IncrementOutboxSucceeded() => System.Threading.Interlocked.Increment(ref _outboxSucceeded);
        public void IncrementOutboxFailed() => System.Threading.Interlocked.Increment(ref _outboxFailed);
        public void IncrementOutboxDead() => System.Threading.Interlocked.Increment(ref _outboxDead);
        public void IncrementNotifierDispatch() => System.Threading.Interlocked.Increment(ref _notifierDispatch);
        public void IncrementSyncHookTimeout() => System.Threading.Interlocked.Increment(ref _syncHookTimeout);
    }
}
