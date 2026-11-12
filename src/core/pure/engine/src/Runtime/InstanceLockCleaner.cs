using System;
using System.Diagnostics;
using System.Threading;

namespace sailflow.engine.Runtime
{
    /// <summary>
    /// 用 <see cref="Timer"/> 周期调用 <see cref="InstanceExecutionLock.CleanupIdle"/>（防重入）。
    /// 进程级单例；未 <see cref="Start"/> 则不清理（与历史「只增不减」兼容）。
    /// </summary>
    public static class InstanceLockCleaner
    {
        static readonly object Gate = new();
        static Timer _timer;
        static InstanceLockCleanerOptions _options = new();
        static int _tickRunning;

        public static bool IsRunning
        {
            get { lock (Gate) return _timer != null; }
        }

        public static void Start(InstanceLockCleanerOptions options = null)
        {
            options ??= new InstanceLockCleanerOptions();
            if (options.Period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options.Period));
            if (options.IdleTtl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(options.IdleTtl));

            lock (Gate)
            {
                _options = options;
                var due = options.Period;
                if (_timer == null)
                {
                    _timer = new Timer(OnTick, null, due, options.Period);
                }
                else
                {
                    _timer.Change(due, options.Period);
                }
            }
        }

        public static void Stop()
        {
            Timer timer;
            lock (Gate)
            {
                timer = _timer;
                _timer = null;
            }

            timer?.Dispose();
        }

        static void OnTick(object state)
        {
            if (Interlocked.Exchange(ref _tickRunning, 1) != 0)
                return;

            try
            {
                InstanceLockCleanerOptions opts;
                lock (Gate)
                    opts = _options ?? new InstanceLockCleanerOptions();

                var n = InstanceExecutionLock.CleanupIdle(opts.IdleTtl);
                if (n > 0)
                {
                    Trace.TraceInformation(
                        "[InstanceExecutionLock] CleanupIdle removed={0} remaining={1} ttl={2}",
                        n, InstanceExecutionLock.Count, opts.IdleTtl);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("[InstanceExecutionLock] CleanupIdle failed: {0}", ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _tickRunning, 0);
            }
        }
    }
}
