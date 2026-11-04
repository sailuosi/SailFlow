using System.Diagnostics;
using saiflow.engine.Client;

namespace saiflow.assignee.Job;

/// <summary>自动审批锚点后台 drain（与 HookOutboxDrainLoop 同心智）。</summary>
public static class AutoApproveAnchorDrainLoop
{
    public static async Task RunAsync(
        SailClient client,
        AutoApproveAnchorOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        options ??= new AutoApproveAnchorOptions();
        var worker = new AutoApproveAnchorWorker(client, options);

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
                Trace.TraceWarning("[Assignee.AutoApprove] drain iteration failed: {0}", ex.Message);
            }

            try
            {
                await Task.Delay(options.PollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public static CancellationTokenSource StartBackground(SailClient client, AutoApproveAnchorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        options ??= new AutoApproveAnchorOptions();
        var cts = new CancellationTokenSource();
        _ = Task.Run(() => RunAsync(client, options, cts.Token), cts.Token);
        return cts;
    }
}
