using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace saiflow.msg.SideEffect
{
    /// <summary>
    /// BestEffort 生产/消费泵：有界 <see cref="Channel{T}"/> + <b>单消费者串行</b>。
    /// 崩溃或进程回收时队列内未消费项可丢；不替代 Outbox。
    /// </summary>
    public sealed class BestEffortChannelPump<T> : IDisposable
    {
        private readonly Channel<T> _channel;
        private readonly Func<T, CancellationToken, Task> _consume;
        private readonly object _gate = new();
        private CancellationTokenSource _cts;
        private Task _loop;
        private int _started;

        public BestEffortChannelPump(BestEffortChannelOptions options, Func<T, CancellationToken, Task> consume)
        {
            if (consume == null)
                throw new ArgumentNullException(nameof(consume));

            options ??= new BestEffortChannelOptions();
            var capacity = Math.Max(1, options.Capacity);
            _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
            {
                FullMode = options.DropWhenFull
                    ? BoundedChannelFullMode.DropWrite
                    : BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            _consume = consume;
        }

        /// <summary>非阻塞入队；满且 DropWrite 时返回 false（消息已丢）。</summary>
        public bool TryWrite(T item) => _channel.Writer.TryWrite(item);

        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            lock (_gate)
            {
                _cts = new CancellationTokenSource();
                _loop = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
            }
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _started, 0) == 0)
                return;

            CancellationTokenSource cts;
            Task loop;
            lock (_gate)
            {
                cts = _cts;
                loop = _loop;
                _cts = null;
                _loop = null;
            }

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
                cts?.Cancel();
            }
            catch
            {
                // ignore
            }

            try
            {
                loop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // BestEffort：关停不等待干净排空
            }

            cts?.Dispose();
        }

        public void Dispose() => Stop();

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        await _consume(item, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"[saiflow.msg] BestEffortChannelPump consume failed: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // shut down
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[saiflow.msg] BestEffortChannelPump loop ended: {ex.Message}");
            }
        }
    }
}
