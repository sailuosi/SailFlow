using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Hooks;

namespace sailflow.biz.Approval;

/// <summary>
/// Sync：引擎终态事件 → <see cref="ISailApprovalBusiness.OnFinish"/> / <see cref="ISailApprovalBusiness.OnAborted"/>。
/// 失败即抛（FailFast）；勿另挂第二套终态回写 Hook。
/// </summary>
public sealed class SailApprovalBusinessLifecycleHook : FlowHookBase
{
    public const string HookName = "sail.approval.lifecycle";

    readonly SailApprovalBusinessRegistry _registry;

    public SailApprovalBusinessLifecycleHook(SailApprovalBusinessRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public SailApprovalBusinessLifecycleHook(IEnumerable<ISailApprovalBusiness> handlers)
        : this(new SailApprovalBusinessRegistry(handlers))
    {
    }

    public override string Name => HookName;
    public override string Title => "审批业务终态回写";

    public override void Handle(FlowHookContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizChannel)) return;
        var handler = _registry.TryGet(ctx.BizChannel);
        if (handler == null) return;
        if (string.IsNullOrWhiteSpace(ctx.BizPK)) return;

        var state = TryReadProcessState(ctx);
        var decision = SailApprovalLifecycleMapper.Map(ctx.EventType, state);
        if (decision.Kind == SailApprovalLifecycleKind.None)
            return;

        var bizCtx = new SailApprovalBusinessContext
        {
            FormCode = handler.FormCode,
            BizChannel = ctx.BizChannel,
            BizPK = ctx.BizPK,
            ProcessId = ctx.ProcessId ?? "",
            Version = string.IsNullOrWhiteSpace(ctx.Version) ? "1" : ctx.Version,
            ProcessInstanceId = string.IsNullOrWhiteSpace(ctx.ProcessInstanceId)
                ? ""
                : ctx.ProcessInstanceId.Trim(),
            FinishEvent = ctx.EventType,
            Outcome = decision.Outcome,
            AbortKind = decision.AbortKind,
            Kit = ctx.Kit
        };

        if (decision.Kind == SailApprovalLifecycleKind.Finish)
            handler.OnFinish(bizCtx);
        else
            handler.OnAborted(bizCtx);
    }

    static ProcessStateEnum? TryReadProcessState(FlowHookContext ctx)
    {
        if (ctx.Kit == null || string.IsNullOrWhiteSpace(ctx.ProcessInstanceId))
            return null;
        try
        {
            var row = ctx.Kit.findRowById<ProcessInstanceEntity>(ctx.ProcessInstanceId.Trim());
            if (row?.ProcessState is short s)
                return (ProcessStateEnum)s;
        }
        catch
        {
            /* 读不到状态时退回纯事件映射 */
        }
        return null;
    }
}
