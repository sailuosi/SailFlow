using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Client;

namespace saiflow.engine.Hooks
{
    /// <summary>Outbox 后台 drain 循环（宿主自行 StartBackground / RunAsync，无 MS Hosting 硬依赖）。</summary>
    public static class HookOutboxDrainLoop
    {
        public static async Task RunAsync(SailClient client, HookOutboxOptions options, CancellationToken cancellationToken = default)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            options ??= new HookOutboxOptions();
            var worker = new HookOutboxWorker(client, options);
            var poll = options.PollInterval;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await worker.ProcessPendingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"[saiflow] Hook outbox drain iteration failed: {ex.Message}");
                }

                try
                {
                    await Task.Delay(poll, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>在后台线程池启动 drain；返回 CTS，宿主 shutdown 时 Cancel。</summary>
        public static CancellationTokenSource StartBackground(SailClient client, HookOutboxOptions options = null)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var cts = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(client, options, cts.Token), cts.Token);
            return cts;
        }
    }
}
