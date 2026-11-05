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

    public Task<int> ProcessPendingAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
                ProcessOne(row, cancellationToken);
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

        return Task.FromResult(processed);
    }

    void ProcessOne(HookOutboxEntity row, CancellationToken ct)
    {
        var om = new HookOutboxManager(Sail);
        var anchor = Deserialize(row);
        if (anchor == null || anchor.ProcessInstanceId is not > 0 || string.IsNullOrWhiteSpace(anchor.ActivityId))
        {
            TerminalSkip(om, row, "Invalid AutoApproveAnchor payload (need ProcessInstanceId+ActivityId).");
            return;
        }

        // Outbox 列优先于 Payload（CreateNewTask 回写）
        if (row.ActivityInstanceId is > 0)
            anchor.ActivityInstanceId = row.ActivityInstanceId;

        TaskEntity? task;
        ActivityInstanceEntity? readyAct;
        string versionHint;
        string userId;
        Dictionary<string, string> dyn;

        using (var probeKit = Sail.UseRunSQL())
        {
            var sibling = SailSqlAccess.Sibling(probeKit);

            if (!IsProcessInstanceRunnable(sibling, anchor.ProcessInstanceId!.Value))
            {
                if (IsProcessInstanceTerminal(sibling, anchor.ProcessInstanceId.Value)
                    || IsAnchorTargetAlreadyLeft(sibling, anchor))
                {
                    MarkDone(om, row, alreadyDone: true);
                    return;
                }

                TerminalSkip(om, row,
                    AssigneeReasonCodes.AutoApproveSkipped + ": process instance not running.");
                return;
            }

            // 幂等：上一轮 Run 已成功但 MarkSucceeded 前崩溃 → lease 回收后目标已离开
            if (IsAnchorTargetAlreadyLeft(sibling, anchor))
            {
                MarkDone(om, row, alreadyDone: true);
                return;
            }

            task = FindWaitingTask(sibling, anchor);
            readyAct = null;
            if (task == null)
            {
                readyAct = FindReadyActivity(sibling, anchor);
                if (readyAct == null)
                {
                    using var kit = Sail.UseRunSQL();
                    kit.beginTransaction();
                    if ((row.Attempt ?? 0) + 1 >= Options.MaxAttempts)
                    {
                        om.MarkSkipped(kit, row,
                            AssigneeReasonCodes.AutoApproveSkipped
                            + ": no Waiting task / Ready activity after retries.");
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
            else if (!IsTaskActivityReady(sibling, task))
            {
                using var kit = Sail.UseRunSQL();
                kit.beginTransaction();
                if ((row.Attempt ?? 0) + 1 >= Options.MaxAttempts)
                {
                    om.MarkSkipped(kit, row,
                        AssigneeReasonCodes.AutoApproveSkipped + ": activity not Ready/Waiting.");
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
            versionHint = string.IsNullOrWhiteSpace(anchor.Version)
                ? ResolveVersionByProcessInstance(sibling, anchor.ProcessInstanceId)
                : anchor.Version!;
            if (RequiresManualNextSelection(activityId, anchor, task, versionHint, out var pickReason))
            {
                TerminalSkip(om, row, AssigneeReasonCodes.AutoApproveSkipped + ": " + pickReason);
                return;
            }

            userId = !string.IsNullOrWhiteSpace(anchor.PreferUserId)
                ? anchor.PreferUserId!
                : task?.AssignedUserId;
            if (string.IsNullOrWhiteSpace(userId))
                userId = "system.autoApprove";

            dyn = new Dictionary<string, string>
            {
                ["sail.autoApprove"] = "1",
                ["sail.autoApprove.strategy"] = anchor.StrategyCode ?? "",
                ["sail.comment"] = string.IsNullOrWhiteSpace(anchor.Reason)
                    ? "策略自动通过（Assignee AutoApprove Job）"
                    : anchor.Reason!
            };
            if (userId.StartsWith("__sail_policy_", StringComparison.OrdinalIgnoreCase))
                dyn["sail.policySkip"] = "1";

            // Run 前再确认目标仍 Waiting/Ready
            if (task != null)
            {
                var stillWaiting = FindWaitingTask(sibling, anchor);
                if (stillWaiting == null || stillWaiting.Id != task.Id)
                {
                    if (IsAnchorTargetAlreadyLeft(sibling, anchor))
                        MarkDone(om, row, alreadyDone: true);
                    else
                        TerminalSkip(om, row,
                            AssigneeReasonCodes.AutoApproveSkipped + ": waiting task disappeared before Run.");
                    return;
                }
            }
            else if (readyAct != null)
            {
                var stillReady = FindReadyActivity(sibling, anchor);
                if (stillReady == null || stillReady.Id != readyAct.Id)
                {
                    if (IsAnchorTargetAlreadyLeft(sibling, anchor))
                        MarkDone(om, row, alreadyDone: true);
                    else
                        TerminalSkip(om, row,
                            AssigneeReasonCodes.AutoApproveSkipped + ": ready activity disappeared before Run.");
                    return;
                }
            }
        } // 释放 probe 连接后再 Run

        var wf = new WorkflowService(Sail);
        WfExecutedResult exec;
        if (task != null)
        {
            var runner = new WfAppRunner
            {
                UserId = userId,
                UserName = anchor.PreferUserName ?? task.AssignedUserName ?? userId,
                ProcessId = anchor.ProcessId ?? task.ProcessId,
                Version = string.IsNullOrWhiteSpace(anchor.Version) ? versionHint : anchor.Version,
                TaskId = task.Id,
                AppName = task.AppName,
                AppInstanceId = task.AppInstanceId,
                DynamicVariables = dyn
            };
            // Job 路径走同步 API（草案阶段 0：去掉伪 async 线程池切换）
            exec = wf.RunProcess(runner);
        }
        else
        {
            var runner = new WfAppRunner
            {
                UserId = userId,
                UserName = anchor.PreferUserName ?? userId,
                ProcessId = anchor.ProcessId ?? readyAct!.ProcessId,
                Version = string.IsNullOrWhiteSpace(anchor.Version) ? versionHint : anchor.Version,
                ActivityInstanceId = readyAct!.Id,
                AppName = readyAct.AppName,
                AppInstanceId = readyAct.AppInstanceId,
                DynamicVariables = dyn
            };
            exec = wf.RunProcessAuto(runner);
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
                    // Run「业务失败」但目标已离开 → 按已成功幂等收口，避免二次 Run
                    if (IsAnchorTargetAlreadyLeft(SailSqlAccess.Sibling(kit), anchor))
                    {
                        om.MarkSucceeded(kit, row);
                    }
                    else
                    {
                        var msg = AssigneeReasonCodes.AutoApproveFailed + ": "
                                  + (exec.Message ?? exec.Status.ToString());
                        if (!string.IsNullOrEmpty(exec.ReasonCode))
                            msg += " ReasonCode=" + exec.ReasonCode;
                        om.MarkFailed(kit, row, msg, Options.MaxAttempts);
                    }
                }

                kit.commit();
            }
            catch
            {
                throw;
            }
        }
    }

    void TerminalSkip(HookOutboxManager om, HookOutboxEntity row, string note)
    {
        using var kit = Sail.UseRunSQL();
        kit.beginTransaction();
        om.MarkSkipped(kit, row, note);
        kit.commit();
    }

    void MarkDone(HookOutboxManager om, HookOutboxEntity row, bool alreadyDone)
    {
        using var kit = Sail.UseRunSQL();
        kit.beginTransaction();
        om.MarkSucceeded(kit, row);
        kit.commit();
        if (alreadyDone)
        {
            Trace.TraceInformation(
                "[Assignee.AutoApprove] row={0} already advanced → MarkSucceeded (idempotent)",
                row.Id);
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

    static TaskEntity? FindWaitingTask(SQLBuilder kit, AutoApproveAnchor anchor)
    {
        var clip = kit.useClip();
        clip.from<TaskEntity>(out var t);
        clip.where(() => t.ProcessInstanceId, anchor.ProcessInstanceId);
        clip.where(() => t.ActivityId, anchor.ActivityId);
        clip.where(() => t.TaskState, (short)TaskStateEnum.Waiting);
        var list = clip.select(t).queryList()?.ToList() ?? new List<TaskEntity>();
        if (list.Count == 0)
            return null;

        if (anchor.TaskId is > 0)
            return list.FirstOrDefault(x => x.Id == anchor.TaskId);

        if (!string.IsNullOrWhiteSpace(anchor.PreferUserId))
        {
            return list.FirstOrDefault(x =>
                string.Equals(x.AssignedUserId, anchor.PreferUserId, StringComparison.OrdinalIgnoreCase));
        }

        return list.Count == 1 ? list[0] : null;
    }

    static bool IsProcessInstanceRunnable(SQLBuilder kit, int processInstanceId)
    {
        try
        {
            var pi = LoadProcessInstance(kit, processInstanceId);
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

    static bool IsProcessInstanceTerminal(SQLBuilder kit, int processInstanceId)
    {
        try
        {
            var pi = LoadProcessInstance(kit, processInstanceId);
            if (pi == null)
                return false;
            return pi.ProcessState == (short)ProcessStateEnum.Completed
                   || pi.ProcessState == (short)ProcessStateEnum.Discarded
                   || pi.ProcessState == (short)ProcessStateEnum.Canceled
                   || pi.ProcessState == (short)ProcessStateEnum.Terminated;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 目标活动已离开 Ready（Completed 等）且无 Waiting 待办 → 视为已代办成功（幂等）。
    /// </summary>
    static bool IsAnchorTargetAlreadyLeft(SQLBuilder kit, AutoApproveAnchor anchor)
    {
        try
        {
            if (anchor.ActivityInstanceId is > 0)
            {
                var clip = kit.useClip();
                clip.from<ActivityInstanceEntity>(out var a);
                clip.where(() => a.Id, anchor.ActivityInstanceId);
                var act = clip.select(a).queryList()?.FirstOrDefault();
                if (act == null)
                    return false;
                return IsLeftReady(act.ActivityState);
            }

            var listClip = kit.useClip();
            listClip.from<ActivityInstanceEntity>(out var ai);
            listClip.where(() => ai.ProcessInstanceId, anchor.ProcessInstanceId);
            listClip.where(() => ai.ActivityId, anchor.ActivityId);
            var list = listClip.select(ai).queryList()?.ToList() ?? new List<ActivityInstanceEntity>();
            if (list.Count == 0)
                return false;

            var anyReady = list.Any(x =>
                x.ActivityState == (short)ActivityStateEnum.Ready
                || x.ActivityState == (short)ActivityStateEnum.Suspended);
            if (anyReady)
                return false;

            return list.Any(x => IsLeftReady(x.ActivityState));
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Assignee.AutoApprove] IsAnchorTargetAlreadyLeft: {0}", ex.Message);
            return false;
        }
    }

    static bool IsLeftReady(short? state) =>
        state == (short)ActivityStateEnum.Completed
        || state == (short)ActivityStateEnum.Withdrawed
        || state == (short)ActivityStateEnum.Sendbacked
        || state == (short)ActivityStateEnum.Cancelled
        || state == (short)ActivityStateEnum.Terminated;

    static bool IsTaskActivityReady(SQLBuilder kit, TaskEntity task)
    {
        if (task.ActivityInstanceId is not > 0)
            return true;
        try
        {
            var clip = kit.useClip();
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
            return true;
        }
    }

    static ActivityInstanceEntity? FindReadyActivity(SQLBuilder kit, AutoApproveAnchor anchor)
    {
        var clip = kit.useClip();
        clip.from<ActivityInstanceEntity>(out var a);
        clip.where(() => a.ProcessInstanceId, anchor.ProcessInstanceId);
        clip.where(() => a.ActivityId, anchor.ActivityId);
        clip.where(() => a.ActivityState, (short)ActivityStateEnum.Ready);
        var list = clip.select(a).queryList()?.ToList() ?? new List<ActivityInstanceEntity>();
        if (list.Count == 0)
            return null;
        if (anchor.ActivityInstanceId is > 0)
            return list.FirstOrDefault(x => x.Id == anchor.ActivityInstanceId);
        // 无 AI Id 且多 Ready：拒绝自动，避免误挑（勿 OrderByDescending）
        return list.Count == 1 ? list[0] : null;
    }

    bool RequiresManualNextSelection(
        string? activityId,
        AutoApproveAnchor anchor,
        TaskEntity? task,
        string version,
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
        if (NextActivityPickProbe.RequiresManualPick(Sail, processId, version, activityId, out var probeReason))
        {
            reason = probeReason;
            return true;
        }

        return false;
    }

    static ProcessInstanceEntity? LoadProcessInstance(SQLBuilder kit, int processInstanceId)
    {
        var clip = kit.useClip();
        clip.from<ProcessInstanceEntity>(out var p);
        clip.where(() => p.Id, processInstanceId);
        return clip.select(p).queryList()?.FirstOrDefault();
    }

    static string ResolveVersionByProcessInstance(SQLBuilder kit, int? processInstanceId)
    {
        try
        {
            if (processInstanceId is not > 0)
                return "1";
            var pi = LoadProcessInstance(kit, processInstanceId.Value);
            return string.IsNullOrWhiteSpace(pi?.Version) ? "1" : pi!.Version;
        }
        catch
        {
            return "1";
        }
    }
}
