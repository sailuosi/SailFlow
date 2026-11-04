using System.Diagnostics;
using System.Text.Json;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Assignee;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Service;

namespace saiflow.assignee.Job;

/// <summary>
/// R10：消费 Kind=<see cref="HookOutboxKinds.AssigneeAutoApprove"/> 的锚点；
/// 苛刻前置不满足则 Postpone / Skip；Run 失败则 Failed 回落手动。
/// </summary>
public sealed class AutoApproveAnchorWorker
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AutoApproveAnchorWorker(SailClient sail, AutoApproveAnchorOptions? options = null)
    {
        Sail = sail ?? throw new ArgumentNullException(nameof(sail));
        Options = options ?? new AutoApproveAnchorOptions();
    }

    public SailClient Sail { get; }
    public AutoApproveAnchorOptions Options { get; }

    public int ProcessPending(int? limit = null, CancellationToken cancellationToken = default) =>
        ProcessPendingAsync(limit, cancellationToken).GetAwaiter().GetResult();

    public async Task<int> ProcessPendingAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var batch = limit ?? Options.BatchSize;
        IList<HookOutboxEntity> claimed;
        var om = new HookOutboxManager(Sail);

        using (var claimKit = Sail.UseRunSQL())
        {
            claimKit.beginTransaction();
            try
            {
                var now = DateTime.UtcNow;
                om.ReclaimStaleProcessing(claimKit, now.AddSeconds(-Math.Max(Options.LeaseSeconds, 30)), now);
                claimed = om.TryClaimPending(
                    claimKit,
                    batch,
                    Options.MaxAttempts,
                    Options.LeaseSeconds,
                    HookOutboxKinds.AssigneeAutoApprove);
                claimKit.commit();
            }
            catch
            {
                throw;
            }
        }

        var processed = 0;
        foreach (var row in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ProcessOneAsync(row, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("[Assignee.AutoApprove] row={0} failed: {1}", row.Id, ex.Message);
                using var kit = Sail.UseRunSQL();
                kit.beginTransaction();
                try
                {
                    om.MarkFailed(kit, row, ex.Message, Options.MaxAttempts);
                    kit.commit();
                }
                catch
                {
                    /* 未 commit 即 Dispose */
                }
            }
        }

        return processed;
    }

    async Task ProcessOneAsync(HookOutboxEntity row, CancellationToken ct)
    {
        var om = new HookOutboxManager(Sail);
        var anchor = Deserialize(row);
        if (anchor == null || anchor.ProcessInstanceId is not > 0 || string.IsNullOrWhiteSpace(anchor.ActivityId))
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            om.MarkFailed(kit, row, "Invalid AutoApproveAnchor payload (need ProcessInstanceId+ActivityId).", Options.MaxAttempts);
            kit.commit();
            return;
        }

        // §11.3 苛刻前置（首批）：须有明确 Waiting 待办 + 可代办用户；否则 Skip/Postpone，回落手动
        var task = FindWaitingTask(anchor);
        if (task == null)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            if ((row.Attempt ?? 0) + 1 >= Options.MaxAttempts)
            {
                om.MarkFailed(kit, row,
                    AssigneeReasonCodes.AutoApproveSkipped + ": no Waiting task after retries.",
                    Options.MaxAttempts);
            }
            else
            {
                row.Attempt = (row.Attempt ?? 0) + 1;
                om.Postpone(kit, row, Options.RetryWhenNoTask, "Waiting task not visible yet.");
            }

            kit.commit();
            return;
        }

        var userId = !string.IsNullOrWhiteSpace(anchor.PreferUserId)
            ? anchor.PreferUserId!
            : task.AssignedUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            om.MarkFailed(kit, row, AssigneeReasonCodes.AutoApproveSkipped + ": no assignee on task.", Options.MaxAttempts);
            kit.commit();
            return;
        }

        var runner = new WfAppRunner
        {
            UserId = userId,
            UserName = anchor.PreferUserName ?? task.AssignedUserName ?? userId,
            ProcessId = anchor.ProcessId ?? task.ProcessId,
            Version = anchor.Version,
            TaskId = task.Id,
            AppName = task.AppName,
            AppInstanceId = task.AppInstanceId,
            DynamicVariables = new Dictionary<string, string>
            {
                ["sail.autoApprove"] = "1",
                ["sail.autoApprove.strategy"] = anchor.StrategyCode ?? "",
                ["sail.comment"] = string.IsNullOrWhiteSpace(anchor.Reason)
                    ? "策略自动通过（Assignee AutoApprove Job）"
                    : anchor.Reason!
            }
        };

        var wf = new WorkflowService(Sail);
        var exec = await wf.RunProcessAsync(runner).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        using (var kit = Sail.UseRunSQL())
        {
            kit.beginTransaction();
            try
            {
                if (exec.Status == WfExecutedStatus.Success)
                {
                    om.MarkSucceeded(kit, row);
                }
                else
                {
                    var msg = AssigneeReasonCodes.AutoApproveFailed + ": " + (exec.Message ?? exec.Status.ToString());
                    if (!string.IsNullOrEmpty(exec.ReasonCode))
                        msg += " ReasonCode=" + exec.ReasonCode;
                    om.MarkFailed(kit, row, msg, Options.MaxAttempts);
                }

                kit.commit();
            }
            catch
            {
                throw;
            }
        }
    }

    AutoApproveAnchor? Deserialize(HookOutboxEntity row)
    {
        if (string.IsNullOrWhiteSpace(row.PayloadJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<AutoApproveAnchor>(row.PayloadJson, JsonOptions);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Assignee.AutoApprove] deserialize failed: {0}", ex.Message);
            return null;
        }
    }

    TaskEntity? FindWaitingTask(AutoApproveAnchor anchor)
    {
        using var kit = Sail.UseRunSQL();
        var clip = SailSqlAccess.Sibling(kit).useClip();
        clip.from<TaskEntity>(out var t);
        clip.where(() => t.ProcessInstanceId, anchor.ProcessInstanceId);
        clip.where(() => t.ActivityId, anchor.ActivityId);
        clip.where(() => t.TaskState, (short)TaskStateEnum.Waiting);
        var list = clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
        if (list.Count == 0)
            return null;

        // 禁止「扫任意 Waiting」：有 TaskId / PreferUserId 时必须命中，否则 null → Skip
        if (anchor.TaskId is > 0)
            return list.FirstOrDefault(x => x.Id == anchor.TaskId);

        if (!string.IsNullOrWhiteSpace(anchor.PreferUserId))
        {
            return list.FirstOrDefault(x =>
                string.Equals(x.AssignedUserId, anchor.PreferUserId, StringComparison.OrdinalIgnoreCase));
        }

        // 仅 ActivityId：取唯一 Waiting；多条则拒绝自动（避免误代办）
        return list.Count == 1 ? list[0] : null;
    }
}
