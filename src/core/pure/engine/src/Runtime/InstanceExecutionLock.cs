using System;
using System.Collections.Concurrent;
using System.Threading;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// 本机按业务键（通常 AppInstanceId）串行 Start/Run/RunAuto。
    /// 字典私有；外部仅可 <see cref="CleanupIdle"/> 按空闲 TTL 销户。
    /// </summary>
    public static class InstanceExecutionLock
    {
        sealed class Entry
        {
            public readonly SemaphoreSlim Sem = new(1, 1);
            public long LastReleaseTicks = Environment.TickCount64;
        }

        static readonly ConcurrentDictionary<string, Entry> Locks =
            new(StringComparer.Ordinal);

        /// <summary>当前锁表条目数（监控用）。</summary>
        public static int Count => Locks.Count;

        /// <summary>获取并进入锁；Dispose 时 Release 并刷新空闲时间戳。</summary>
        public static IDisposable Acquire(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Instance lock key is required.", nameof(key));

            var entry = Locks.GetOrAdd(key, _ => new Entry());
            entry.Sem.Wait();
            return new Lease(entry);
        }

        /// <summary>
        /// 清理空闲超过 <paramref name="idleTtl"/> 且无人占用（CurrentCount==1）的条目。
        /// 返回实际 Dispose 的数量。
        /// </summary>
        public static int CleanupIdle(TimeSpan idleTtl)
        {
            if (idleTtl <= TimeSpan.Zero)
                return 0;

            var now = Environment.TickCount64;
            var ttlMs = (long)idleTtl.TotalMilliseconds;
            if (ttlMs <= 0)
                return 0;

            var removed = 0;
            foreach (var kv in Locks)
            {
                var entry = kv.Value;
                if (entry.Sem.CurrentCount != 1)
                    continue;

                var last = Volatile.Read(ref entry.LastReleaseTicks);
                var idle = now - last;
                if (idle < ttlMs)
                    continue;

                if (!Locks.TryRemove(kv.Key, out var taken) || !ReferenceEquals(taken, entry))
                {
                    if (taken != null)
                        Locks.TryAdd(kv.Key, taken);
                    continue;
                }

                // Remove 后再确认一次空闲，避免与 Acquire 竞态
                if (taken.Sem.CurrentCount != 1)
                {
                    Locks.TryAdd(kv.Key, taken);
                    continue;
                }

                try
                {
                    taken.Sem.Dispose();
                    removed++;
                }
                catch
                {
                    /* ignore dispose races */
                }
            }

            return removed;
        }

        sealed class Lease : IDisposable
        {
            Entry _entry;
            int _disposed;

            public Lease(Entry entry)
            {
                _entry = entry;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                var entry = _entry;
                _entry = null;
                if (entry == null)
                    return;

                Volatile.Write(ref entry.LastReleaseTicks, Environment.TickCount64);
                try
                {
                    entry.Sem.Release();
                }
                catch (ObjectDisposedException)
                {
                    // 极端：清理与 Release 交错；锁已销户，忽略
                }
            }
        }
    }
}
