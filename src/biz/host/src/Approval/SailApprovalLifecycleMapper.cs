using sailflow.utils;
using sailflow.events;

namespace sailflow.biz.Approval;

/// <summary>
/// Q8：引擎终态信号 → OnFinish / OnAborted。
/// 有 <see cref="ProcessStateEnum"/> 时以状态为「是否到 End」校正；事件补 Outcome / AbortKind。
/// </summary>
public static class SailApprovalLifecycleMapper
{
    /// <summary>
    /// 映射终态回调。SendBack / 运行中驳回落点 / 步骤撤回等非终态返回 <see cref="SailApprovalLifecycleKind.None"/>。
    /// </summary>
    public static SailApprovalLifecycleDecision Map(
        EventFireTypeEnum? eventType,
        ProcessStateEnum? processState = null)
    {
        if (processState.HasValue)
        {
            switch (processState.Value)
            {
                case ProcessStateEnum.Completed:
                    return SailApprovalLifecycleDecision.Finish(
                        eventType == EventFireTypeEnum.OnProcessRejected
                            ? ApprovalOutcomeCodes.Rejected
                            : ApprovalOutcomeCodes.Approved);

                case ProcessStateEnum.Canceled:
                    return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Cancel);

                case ProcessStateEnum.Discarded:
                    return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Discard);

                case ProcessStateEnum.Terminated:
                    return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Terminate);

                case ProcessStateEnum.Running:
                case ProcessStateEnum.Ready:
                case ProcessStateEnum.Suspended:
                case ProcessStateEnum.NotStart:
                    // 非终态：仅当事件本身是明确的「未到 End 终局」且无状态冲突时才 Abort；
                    // OnProcessRejected / OnProcessWithdrawn 在 Running 下不是单据终态。
                    if (eventType is EventFireTypeEnum.OnProcessClosed)
                        return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Close);
                    if (eventType is EventFireTypeEnum.OnProcessCanceled)
                        return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Cancel);
                    if (eventType is EventFireTypeEnum.OnProcessDiscarded)
                        return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Discard);
                    if (eventType is EventFireTypeEnum.OnProcessTerminated)
                        return SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Terminate);
                    return SailApprovalLifecycleDecision.Ignore;
            }
        }

        if (!eventType.HasValue)
            return SailApprovalLifecycleDecision.Ignore;

        return eventType.Value switch
        {
            EventFireTypeEnum.OnProcessCompleted =>
                SailApprovalLifecycleDecision.Finish(ApprovalOutcomeCodes.Approved),

            // 无状态时按「驳回办结」信任事件（与历史 FormBiz 一致）；有 Running 时上面已 Ignore
            EventFireTypeEnum.OnProcessRejected =>
                SailApprovalLifecycleDecision.Finish(ApprovalOutcomeCodes.Rejected),

            EventFireTypeEnum.OnProcessClosed =>
                SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Close),

            EventFireTypeEnum.OnProcessCanceled =>
                SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Cancel),

            EventFireTypeEnum.OnProcessDiscarded =>
                SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Discard),

            EventFireTypeEnum.OnProcessTerminated =>
                SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Terminate),

            // 步骤撤回（Transition）默认非终态；仅无状态且产品把 Withdraw 当整单撤销时才 Abort
            EventFireTypeEnum.OnProcessWithdrawn =>
                SailApprovalLifecycleDecision.Abort(ApprovalAbortKind.Withdraw),

            _ => SailApprovalLifecycleDecision.Ignore
        };
    }
}
