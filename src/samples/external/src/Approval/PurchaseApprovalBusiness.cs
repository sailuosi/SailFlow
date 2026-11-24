using System;
using System.Globalization;
using System.Text.Json;
using sailflow.biz.Approval;
using sailflow.hooks;

namespace sailflow.external.Approval;

/// <summary>
/// 文档样例：采购申请审批业务门面（与讨论稿同构）。
/// <para>
/// 注册：
/// <c>builder.AddApprovalBusiness(_ =&gt; new PurchaseApprovalBusiness(store)).UseApprovalBusiness();</c>
/// </para>
/// <para>
/// 终态由门面 Sync Hook 分发：到 End → <see cref="OnFinish"/>；未到 End → <see cref="OnAborted"/>。
/// 强一致回写须用 <c>ctx.Kit</c>（本样例为内存仓，仅演示回调形状）。
/// </para>
/// </summary>
public sealed class PurchaseApprovalBusiness : SailApprovalBusinessBase
{
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    readonly PurchaseStore _store;

    public PurchaseApprovalBusiness()
        : this(new PurchaseStore())
    {
    }

    public PurchaseApprovalBusiness(PurchaseStore store)
        => _store = store ?? throw new ArgumentNullException(nameof(store));

    public override string FormCode => PurchaseStatusCodes.FormCode;

    public override void OnInit(SailApprovalBusinessContext ctx)
    {
        ctx.Data = new { title = "", amount = 0m };
    }

    public override void OnQuery(SailApprovalBusinessContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizPK))
        {
            ctx.Error = "BizPK required";
            return;
        }
        ctx.Data = _store.Get(ctx.BizPK);
        if (ctx.Data == null)
            ctx.Error = "not found";
    }

    public override void OnSubmit(SailApprovalBusinessContext ctx)
    {
        var req = Parse(ctx.PayloadJson);
        if (req.Amount <= 0)
        {
            ctx.Error = "Amount must be > 0";
            return;
        }

        var row = _store.Insert(new PurchaseRecord
        {
            Title = string.IsNullOrWhiteSpace(req.Title) ? "采购申请" : req.Title.Trim(),
            Amount = req.Amount,
            Status = PurchaseStatusCodes.Running
        });

        ctx.BizChannel = FormCode;
        ctx.BizPK = row.Id;
        ctx.BizName = row.Title;
        // 金额进条件袋，供网关/分支求值（引擎不解释业务 Status）
        ctx.Conditions["amount"] = row.Amount.ToString(CultureInfo.InvariantCulture);
        ctx.Data = row;
    }

    public override void OnStarted(SailApprovalBusinessContext ctx)
    {
        if (ctx.Data is not PurchaseRecord row) return;
        if (string.IsNullOrWhiteSpace(ctx.ProcessInstanceId)) return;
        row.ProcessInstanceId = ctx.ProcessInstanceId.Trim();
        _store.Update(row);
        ctx.Data = row;
    }

    public override void OnConsent(SailApprovalBusinessContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizPK)) return;
        _store.ApplyStatus(ctx.BizPK, PurchaseStatusCodes.Running, "审批中");
    }

    public override void OnBackToModify(SailApprovalBusinessContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizPK)) return;
        _store.ApplyStatus(ctx.BizPK, PurchaseStatusCodes.BackToModify, "退回修改");
    }

    public override HookResult? OnCompleting(FlowOperation operation, SailApprovalBusinessContext ctx)
    {
        // 同意办理前：金额须仍 > 0（示意；真实场景可读库校验）
        if (operation != FlowOperation.Run)
            return null;
        if (string.IsNullOrWhiteSpace(ctx.BizPK))
            return HookResult.Reject("BizPK required", "PURCHASE_BIZPK");

        var row = _store.Get(ctx.BizPK);
        if (row == null)
            return HookResult.Reject("Purchase not found", "PURCHASE_NOT_FOUND");
        if (row.Amount <= 0)
            return HookResult.Reject("Amount must be > 0", "PURCHASE_AMOUNT");
        if (row.Status is PurchaseStatusCodes.Approved or PurchaseStatusCodes.Rejected or PurchaseStatusCodes.Aborted)
            return HookResult.Reject("Already finished", "PURCHASE_FINISHED");

        return HookResult.Ok();
    }

    public override void OnFinish(SailApprovalBusinessContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizPK)) return;

        if (string.Equals(ctx.Outcome, ApprovalOutcomeCodes.Approved, StringComparison.OrdinalIgnoreCase))
        {
            _store.ApplyStatus(ctx.BizPK, PurchaseStatusCodes.Approved, "已通过");
            return;
        }

        if (string.Equals(ctx.Outcome, ApprovalOutcomeCodes.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            _store.ApplyStatus(ctx.BizPK, PurchaseStatusCodes.Rejected, "已驳回");
            return;
        }
    }

    public override void OnAborted(SailApprovalBusinessContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.BizPK)) return;
        var text = ctx.AbortKind switch
        {
            ApprovalAbortKind.Withdraw => "已撤回",
            ApprovalAbortKind.Discard => "已作废",
            ApprovalAbortKind.Terminate => "已终止",
            ApprovalAbortKind.Cancel => "已取消",
            _ => "已关闭"
        };
        _store.ApplyStatus(ctx.BizPK, PurchaseStatusCodes.Aborted, text);
    }

    static PurchaseSubmitPayload Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PurchaseSubmitPayload();
        try
        {
            return JsonSerializer.Deserialize<PurchaseSubmitPayload>(json, JsonOpts)
                   ?? new PurchaseSubmitPayload();
        }
        catch
        {
            return new PurchaseSubmitPayload();
        }
    }

    sealed class PurchaseSubmitPayload
    {
        public string? Title { get; set; }
        public decimal Amount { get; set; }
    }
}
