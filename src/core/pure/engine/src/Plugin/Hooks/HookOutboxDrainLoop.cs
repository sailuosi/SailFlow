using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Client;

namespace sailflow.engine.Hooks
{
    /// <summary>Outbox 后台 drain 循环（宿主自行 StartBackground / RunAsync，无 MS Hosting 硬依赖）。</summary>
    public static class HookOutboxDrainLoop
    {
        public static async Task RunAsync(SailClient client, HookOutboxOptions options, CancellationToken cancellationToken = default)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            options ??= client.HookOutboxOptions ?? new HookOutboxOptions();
            var worker = new HookOutboxWorker(client, options);
            var poll = options.PollInterval;
            var purgeEvery = Math.Max(1, options.PurgeEveryDrainCycles);
            var cycle = 0;

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
                    Trace.TraceWarning($"[sailflow] Hook outbox drain iteration failed: {ex.Message}");
                }

                cycle++;
                if (cycle % purgeEvery == 0)
                {
                    try
                    {
                        var purged = worker.PurgeExpiredTerminal(cancellationToken);
                        if (purged > 0)
                            Trace.TraceInformation($"[sailflow] Hook outbox TTL purged {purged} row(s).");
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"[sailflow] Hook outbox TTL purge failed: {ex.Message}");
                    }
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

            options ??= client.HookOutboxOptions ?? new HookOutboxOptions();
            var cts = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(client, options, cts.Token), cts.Token);
            return cts;
        }
    }
}
