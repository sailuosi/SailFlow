using System;
using System.Collections.Generic;
using sailflow.engine.Client;
using sailflow.utils;
using sailflow.data;
using sailflow.factory;

namespace sailflow.lanes;
/// <summary>
/// 驳回落点解析：按 <see cref="RejectModeCodes"/> 算出唯一 ActivityId + 历史办理人（覆盖客户端传入）。
/// </summary>
internal static class RejectTargetResolver
{
    public sealed class ResolveResult
    {
        public bool Ok { get; init; }
        public string? ActivityId { get; init; }
        public PerformerList? Performers { get; init; }
        public string? ReasonCode { get; init; }
        public string? Message { get; init; }
    }

    public static string ReadMode(FlowCommandContext runner)
    {
        var raw = runner?.ControlParameterSheet?.RejectMode;
        var normalized = RejectModeCodes.Normalize(raw);
        return normalized;
    }

    public static ResolveResult ResolveBackwardTarget(SailClient sail, FlowCommandContext runner)
    {
        var mode = ReadMode(runner);
        if (string.IsNullOrEmpty(mode))
        {
            return Fail(RejectReasonCodes.RejectModeInvalid,
                "RejectMode invalid. Use Terminate|Previous|Starter|Specified.");
        }

        if (string.Equals(mode, RejectModeCodes.Terminate, StringComparison.OrdinalIgnoreCase))
        {
            return Fail(RejectReasonCodes.RejectModeInvalid,
                "Terminate mode must not call ResolveBackwardTarget.");
        }

        if (string.Equals(mode, RejectModeCodes.Starter, StringComparison.OrdinalIgnoreCase))
            return ResolveStarter(sail, runner);

        if (string.Equals(mode, RejectModeCodes.Specified, StringComparison.OrdinalIgnoreCase))
            return ResolveSpecified(sail, runner);

        // Previous（默认）
        return ResolvePrevious(sail, runner);
    }

    static ResolveResult ResolveStarter(SailClient sail, FlowCommandContext runner)
    {
        var pm = sail.ProcessModelFactory.CreateByProcess(runner.ProcessId, runner.Version);
        var first = pm?.GetFirstActivity();
        if (first == null || string.IsNullOrWhiteSpace(first.ActivityId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing, "Reject Starter: first activity not found.");
        }

        var aim = sail.ManagerFactory.Activities;
        ActivityInstanceEntity firstAi = null;
        if (!string.IsNullOrWhiteSpace(runner.BizPK))
            firstAi = aim.GetActivityInstanceLatest(runner.BizPK, runner.ProcessId, first.ActivityId);

        var userId = firstAi?.CreatedUserId;
        var userName = firstAi?.CreatedUserName;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Starter: first activity instance / creator missing.");
        }

        return Ok(first.ActivityId, userId, userName ?? userId);
    }

    static ResolveResult ResolvePrevious(SailClient sail, FlowCommandContext runner)
    {
        var aim = sail.ManagerFactory.Activities;
        TaskViewEntity taskView;
        var runningNode = aim.GetRunningNode(runner, out taskView);
        if (runningNode == null)
        {
            return Fail(RejectReasonCodes.RejectTargetMissing, "Reject Previous: running node not found.");
        }

        var npc = sail.ManagerFactory.PreviousSteps;
        bool hasGatewayNode;
        var previousList = npc.GetPreviousActivityInstanceList(runningNode, true, out hasGatewayNode);
        var previous = (previousList != null && previousList.Count > 0) ? previousList[0] : null;
        if (previous == null || string.IsNullOrWhiteSpace(previous.ActivityId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Previous: no previous task activity on execution chain.");
        }

        var userId = previous.EndedUserId;
        var userName = previous.EndedUserName;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Previous: previous activity EndedUser missing.");
        }

        return Ok(previous.ActivityId, userId, userName ?? userId);
    }

    static ResolveResult ResolveSpecified(SailClient sail, FlowCommandContext runner)
    {
        var activityId = runner?.ControlParameterSheet?.RejectReturnActivityId?.Trim();
        if (string.IsNullOrEmpty(activityId))
        {
            return Fail(RejectReasonCodes.RejectReturnActivityRequired,
                "Reject Specified requires ControlParameterSheet.RejectReturnActivityId.");
        }

        var aim = sail.ManagerFactory.Activities;
        TaskViewEntity taskView;
        var runningNode = aim.GetRunningNode(runner, out taskView);
        if (runningNode == null || string.IsNullOrWhiteSpace(runningNode.ProcessInstanceId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing, "Reject Specified: running node not found.");
        }

        var jumpBack = aim.GetActivityInstanceLatest(runningNode.ProcessInstanceId, activityId);
        if (jumpBack == null)
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Specified: activity instance not found for " + activityId);
        }

        if (jumpBack.ActivityState != (short)ActivityStateEnum.Completed)
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Specified: target activity instance is not Completed.");
        }

        var userId = jumpBack.EndedUserId;
        var userName = jumpBack.EndedUserName;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Fail(RejectReasonCodes.RejectTargetMissing,
                "Reject Specified: target EndedUser missing.");
        }

        return Ok(activityId, userId, userName ?? userId);
    }

    static ResolveResult Ok(string activityId, string userId, string userName)
    {
        var list = new PerformerList();
        list.Add(new Performer(userId, userName));
        return new ResolveResult
        {
            Ok = true,
            ActivityId = activityId,
            Performers = list
        };
    }

    static ResolveResult Fail(string reason, string message) =>
        new()
        {
            Ok = false,
            ReasonCode = reason,
            Message = message
        };
}
