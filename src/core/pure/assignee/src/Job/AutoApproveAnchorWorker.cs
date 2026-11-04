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
using saiflow.engine.Xpdl;

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

        // §11.3 苛刻前置：实例须运行中；须有明确 Waiting 待办 + 可代办用户；否则 Skip/Postpone
        if (!IsProcessInstanceRunnable(anchor.ProcessInstanceId!.Value))
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            om.MarkFailed(kit, row,
                AssigneeReasonCodes.AutoApproveSkipped + ": process instance not running.",
                Options.MaxAttempts);
            kit.commit();
            return;
        }

        var task = FindWaitingTask(anchor);
        ActivityInstanceEntity? readyAct = null;
        if (task == null)
        {
            // R10 零 Performer：无待办，按 Ready 活动实例 RunProcessAuto
            readyAct = FindReadyActivity(anchor);
            if (readyAct == null)
            {
                using var kit = Sail.UseRunSQL();
                kit.beginTransaction();
                if ((row.Attempt ?? 0) + 1 >= Options.MaxAttempts)
                {
                    om.MarkFailed(kit, row,
                        AssigneeReasonCodes.AutoApproveSkipped + ": no Waiting task / Ready activity after retries.",
                        Options.MaxAttempts);
                }
                else
                {
                    row.Attempt = (row.Attempt ?? 0) + 1;
                    om.Postpone(kit, row, Options.RetryWhenNoTask, "Task/activity not visible yet.");
                }

                kit.commit();
                return;
            }
        }
        else if (!IsTaskActivityReady(task))
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            if ((row.Attempt ?? 0) + 1 >= Options.MaxAttempts)
            {
                om.MarkFailed(kit, row,
                    AssigneeReasonCodes.AutoApproveSkipped + ": activity not Ready/Waiting.",
                    Options.MaxAttempts);
            }
            else
            {
                row.Attempt = (row.Attempt ?? 0) + 1;
                om.Postpone(kit, row, Options.RetryWhenNoTask, "Activity instance not ready yet.");
            }

            kit.commit();
            return;
        }

        var activityId = task?.ActivityId ?? readyAct?.ActivityId ?? anchor.ActivityId;
        if (RequiresManualNextSelection(activityId, anchor, task, out var pickReason))
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            om.MarkFailed(kit, row,
                AssigneeReasonCodes.AutoApproveSkipped + ": " + pickReason,
                Options.MaxAttempts);
            kit.commit();
            return;
        }

        var userId = !string.IsNullOrWhiteSpace(anchor.PreferUserId)
            ? anchor.PreferUserId!
            : task?.AssignedUserId;
        if (string.IsNullOrWhiteSpace(userId))
            userId = "system.autoApprove";

        var dyn = new Dictionary<string, string>
        {
            ["sail.autoApprove"] = "1",
            ["sail.autoApprove.strategy"] = anchor.StrategyCode ?? "",
            ["sail.comment"] = string.IsNullOrWhiteSpace(anchor.Reason)
                ? "策略自动通过（Assignee AutoApprove Job）"
                : anchor.Reason!
        };
        if (userId.StartsWith("__sail_policy_", StringComparison.OrdinalIgnoreCase))
            dyn["sail.policySkip"] = "1";

        var wf = new WorkflowService(Sail);
        WfExecutedResult exec;
        if (task != null)
        {
            var runner = new WfAppRunner
            {
                UserId = userId,
                UserName = anchor.PreferUserName ?? task.AssignedUserName ?? userId,
                ProcessId = anchor.ProcessId ?? task.ProcessId,
                Version = string.IsNullOrWhiteSpace(anchor.Version) ? ResolveVersion(task) : anchor.Version,
                TaskId = task.Id,
                AppName = task.AppName,
                AppInstanceId = task.AppInstanceId,
                DynamicVariables = dyn
            };
            exec = await wf.RunProcessAsync(runner).ConfigureAwait(false);
        }
        else
        {
            var runner = new WfAppRunner
            {
                UserId = userId,
                UserName = anchor.PreferUserName ?? userId,
                ProcessId = anchor.ProcessId ?? readyAct!.ProcessId,
                Version = string.IsNullOrWhiteSpace(anchor.Version)
                    ? ResolveVersionByProcessInstance(readyAct!.ProcessInstanceId)
                    : anchor.Version,
                ActivityInstanceId = readyAct!.Id,
                AppName = readyAct.AppName,
                AppInstanceId = readyAct.AppInstanceId,
                DynamicVariables = dyn
            };
            exec = await wf.RunProcessAutoAsync(runner).ConfigureAwait(false);
        }

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

    bool IsProcessInstanceRunnable(int processInstanceId)
    {
        try
        {
            using var kit = Sail.UseRunSQL();
            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var p);
            clip.where(() => p.Id, processInstanceId);
            var pi = clip.select(p).queryList()?.FirstOrDefault();
            if (pi == null)
                return false;
            return pi.ProcessState == (short)ProcessStateEnum.Running
                   || pi.ProcessState == (short)ProcessStateEnum.Ready;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Assignee.AutoApprove] read ProcessInstance failed: {0}", ex.Message);
            return false;
        }
    }

    bool IsTaskActivityReady(TaskEntity task)
    {
        if (task.ActivityInstanceId is not > 0)
            return true; // 无活动实例 id 时不阻断
        try
        {
            using var kit = Sail.UseRunSQL();
            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.from<ActivityInstanceEntity>(out var a);
            clip.where(() => a.Id, task.ActivityInstanceId);
            var act = clip.select(a).queryList()?.FirstOrDefault();
            if (act == null)
                return false;
            return act.ActivityState == (short)ActivityStateEnum.Ready
                   || act.ActivityState == (short)ActivityStateEnum.Suspended;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Assignee.AutoApprove] read ActivityInstance failed: {0}", ex.Message);
            return true; // 读失败不硬拦，交给 Run
        }
    }

    /// <summary>
    /// Extra 显式标记，或下一跳结构要求运行时选人 / 多出口人工。
    /// </summary>
    ActivityInstanceEntity? FindReadyActivity(AutoApproveAnchor anchor)
    {
        using var kit = Sail.UseRunSQL();
        var clip = SailSqlAccess.Sibling(kit).useClip();
        clip.from<ActivityInstanceEntity>(out var a);
        clip.where(() => a.ProcessInstanceId, anchor.ProcessInstanceId);
        clip.where(() => a.ActivityId, anchor.ActivityId);
        clip.where(() => a.ActivityState, (short)ActivityStateEnum.Ready);
        var list = clip.select(a).queryList()?.ToList() ?? new List<ActivityInstanceEntity>();
        if (list.Count == 0)
            return null;
        if (anchor.ActivityInstanceId is > 0)
            return list.FirstOrDefault(x => x.Id == anchor.ActivityInstanceId);
        return list.Count == 1 ? list[0] : list.OrderByDescending(x => x.Id).FirstOrDefault();
    }

    bool RequiresManualNextSelection(
        string? activityId,
        AutoApproveAnchor anchor,
        TaskEntity? task,
        out string reason)
    {
        reason = "next performers incomplete (manual pick required).";
        if (anchor.Extra != null
            && anchor.Extra.TryGetValue(NextActivityPickProbe.ExtraIncompleteKey, out var flag)
            && (flag == "1" || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)))
        {
            if (anchor.Extra.TryGetValue(NextActivityPickProbe.ExtraReasonKey, out var r)
                && !string.IsNullOrWhiteSpace(r))
                reason = r!;
            return true;
        }

        var processId = anchor.ProcessId ?? task?.ProcessId;
        var version = string.IsNullOrWhiteSpace(anchor.Version)
            ? (task != null ? ResolveVersion(task) : ResolveVersionByProcessInstance(anchor.ProcessInstanceId))
            : anchor.Version!;
        if (NextActivityPickProbe.RequiresManualPick(Sail, processId, version, activityId, out var probeReason))
        {
            reason = probeReason;
            return true;
        }

        return false;
    }

    string ResolveVersion(TaskEntity task) =>
        ResolveVersionByProcessInstance(task.ProcessInstanceId);

    string ResolveVersionByProcessInstance(int? processInstanceId)
    {
        try
        {
            if (processInstanceId is not > 0)
                return "1";
            using var kit = Sail.UseRunSQL();
            var clip = SailSqlAccess.Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var p);
            clip.where(() => p.Id, processInstanceId);
            var pi = clip.select(p).queryList()?.FirstOrDefault();
            return string.IsNullOrWhiteSpace(pi?.Version) ? "1" : pi!.Version;
        }
        catch
        {
            return "1";
        }
    }
}
