using System;
using System.Collections.Generic;
using sailflow.engine.Hooks;

namespace sailflow.biz.Approval;

/// <summary>
/// Completing Gate：按 BizChannel 调 <see cref="ISailApprovalBusiness.OnCompleting"/>（操作白名单）。
/// </summary>
public sealed class SailApprovalCompletingGate : IFlowGate
{
    public const string GateName = "sail.approval.completing";

    /// <summary>白名单：同意办理 / 转办 / 加签 / 撤回 / 驳回 / 关闭 / 取消 / 作废 / 终止 / 改派等。</summary>
    public static readonly HashSet<FlowOperation> DefaultOperations = new()
    {
        FlowOperation.Run,
        FlowOperation.Transfer,
        FlowOperation.SignForward,
        FlowOperation.Withdraw,
        FlowOperation.Reject,
        FlowOperation.Close,
        FlowOperation.Cancel,
        FlowOperation.Discard,
        FlowOperation.Terminate,
        FlowOperation.ChangeAssignee,
        FlowOperation.Delegate,
        FlowOperation.SendBack
    };

    readonly SailApprovalBusinessRegistry _registry;
    readonly HashSet<FlowOperation> _ops;

    public SailApprovalCompletingGate(
        SailApprovalBusinessRegistry registry,
        IEnumerable<FlowOperation>? operations = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _ops = operations != null
            ? new HashSet<FlowOperation>(operations)
            : DefaultOperations;
    }

    public SailApprovalCompletingGate(IEnumerable<ISailApprovalBusiness> handlers)
        : this(new SailApprovalBusinessRegistry(handlers))
    {
    }

    public string Name => GateName;
    public string Title => "审批业务办理前闸门";

    public HookResult Validate(FlowHookContext ctx)
    {
        if (!_ops.Contains(ctx.Operation))
            return HookResult.Ok();
        if (string.IsNullOrWhiteSpace(ctx.BizChannel))
            return HookResult.Ok();

        var handler = _registry.TryGet(ctx.BizChannel);
        if (handler == null)
            return HookResult.Ok();

        var bizCtx = new SailApprovalBusinessContext
        {
            FormCode = handler.FormCode,
            BizChannel = ctx.BizChannel,
            BizPK = ctx.BizPK ?? "",
            ProcessId = ctx.ProcessId ?? "",
            Version = string.IsNullOrWhiteSpace(ctx.Version) ? "1" : ctx.Version,
            ProcessInstanceId = ctx.ProcessInstanceId ?? "",
            CompletingOperation = ctx.Operation,
            Kit = ctx.Kit
        };

        var result = handler.OnCompleting(ctx.Operation, bizCtx);
        return result ?? HookResult.Ok();
    }
}
