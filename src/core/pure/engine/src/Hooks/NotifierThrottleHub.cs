using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// Client 级 Notifier 限流枢纽：按 handler 名一车道（Channel + MaxDegree workers）。
    /// </summary>
    public sealed class NotifierThrottleHub : IDisposable
    {
        private readonly ConcurrentDictionary<string, NotifierThrottleLane> _lanes =
            new(StringComparer.OrdinalIgnoreCase);

        private int _disposed;

        public Task RunAsync(
            string notifierName,
            NotifyThrottleOptions options,
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(notifierName))
                throw new ArgumentException("Notifier name is required.", nameof(notifierName));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (options == null)
                return action(cancellationToken);

            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException(nameof(NotifierThrottleHub));

            var lane = _lanes.GetOrAdd(
                notifierName,
                name => new NotifierThrottleLane(name, options.Normalized()));

            return lane.RunAsync(action, cancellationToken);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            foreach (var lane in _lanes.Values)
            {
                try
                {
                    lane.Dispose();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[sailflow] NotifierThrottleLane dispose failed: {ex.Message}");
                }
            }

            _lanes.Clear();
        }
    }

    /// <summary>单个 Notifier 的有界 Channel + 多消费者车道。</summary>
    internal sealed class NotifierThrottleLane : IDisposable
    {
        private readonly Channel<ThrottleWork> _channel;
        private readonly TimeSpan _minInterval;
        private readonly object _intervalGate = new();
        private DateTime _nextAllowedUtc = DateTime.MinValue;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task[] _workers;
        private int _disposed;

        public NotifierThrottleLane(string name, NotifyThrottleOptions options)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            options ??= new NotifyThrottleOptions().Normalized();
            options = options.Normalized();

            _minInterval = options.MinInterval;
            _channel = Channel.CreateBounded<ThrottleWork>(new BoundedChannelOptions(options.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = options.MaxDegree == 1,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });

            _workers = new Task[options.MaxDegree];
            for (var i = 0; i < _workers.Length; i++)
            {
                var workerId = i;
                _workers[i] = Task.Run(
                    () => WorkerLoopAsync(workerId, _cts.Token),
                    _cts.Token);
            }
        }

        public string Name { get; }

        public async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) == 1)
                throw new ObjectDisposedException($"NotifierThrottleLane:{Name}");

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var work = new ThrottleWork(action, tcs, cancellationToken);

            await _channel.Writer.WriteAsync(work, cancellationToken).ConfigureAwait(false);
            await tcs.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            try
            {
                _channel.Writer.TryComplete();
            }
            catch
            {
                // ignore
            }

            try
            {
                _cts.Cancel();
            }
            catch
            {
                // ignore
            }

            try
            {
                Task.WaitAll(_workers, TimeSpan.FromSeconds(2));
            }
            catch
            {
                // shut down best-effort
            }

            _cts.Dispose();
        }

        private async Task WorkerLoopAsync(int workerId, CancellationToken stoppingToken)
        {
            _ = workerId;
            try
            {
                await foreach (var work in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    if (work.CallerToken.IsCancellationRequested)
                    {
                        work.Completion.TrySetCanceled(work.CallerToken);
                        continue;
                    }

                    try
                    {
                        await DelayForMinIntervalAsync(work.CallerToken).ConfigureAwait(false);

                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                            stoppingToken,
                            work.CallerToken);
                        await work.Action(linked.Token).ConfigureAwait(false);
                        work.Completion.TrySetResult(true);
                    }
                    catch (OperationCanceledException oce)
                    {
                        work.Completion.TrySetCanceled(oce.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        work.Completion.TrySetException(ex);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shut down
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(
                    $"[sailflow] NotifierThrottleLane '{Name}' worker stopped: {ex.Message}");
            }
        }

        private async Task DelayForMinIntervalAsync(CancellationToken cancellationToken)
        {
            if (_minInterval <= TimeSpan.Zero)
                return;

            TimeSpan delay;
            lock (_intervalGate)
            {
                var now = DateTime.UtcNow;
                if (now < _nextAllowedUtc)
                    delay = _nextAllowedUtc - now;
                else
                    delay = TimeSpan.Zero;

                var start = now + delay;
                _nextAllowedUtc = start + _minInterval;
            }

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        private readonly struct ThrottleWork
        {
            public ThrottleWork(
                Func<CancellationToken, Task> action,
                TaskCompletionSource<bool> completion,
                CancellationToken callerToken)
            {
                Action = action;
                Completion = completion;
                CallerToken = callerToken;
            }

            public Func<CancellationToken, Task> Action { get; }
            public TaskCompletionSource<bool> Completion { get; }
            public CancellationToken CallerToken { get; }
        }
    }
}
