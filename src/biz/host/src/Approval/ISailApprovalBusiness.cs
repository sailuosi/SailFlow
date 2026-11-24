using sailflow.hooks;

namespace sailflow.biz.Approval;

/// <summary>
/// 审批业务门面：一业务一类（FormCode = BizChannel）。
/// 终态二分：到 End → <see cref="OnFinish"/>；未到 End → <see cref="OnAborted"/>。
/// </summary>
public interface ISailApprovalBusiness
{
    /// <summary>路由键（与 Runner.BizChannel / 发起 FormCode 对齐）。</summary>
    string FormCode { get; }

    void OnInit(SailApprovalBusinessContext ctx);
    void OnQuery(SailApprovalBusinessContext ctx);

    /// <summary>首次提交：落业务表并写出 BizPK / Conditions；Start 由 Host 模板调用。</summary>
    void OnSubmit(SailApprovalBusinessContext ctx);

    /// <summary>StartProcess 成功后回写实例 Id 等。</summary>
    void OnStarted(SailApprovalBusinessContext ctx);

    void OnConsent(SailApprovalBusinessContext ctx);
    void OnBackToModify(SailApprovalBusinessContext ctx);
    void OnCancel(SailApprovalBusinessContext ctx);

    /// <summary>到达结束节点的办结；<see cref="SailApprovalBusinessContext.Outcome"/> 区分通过/驳回。</summary>
    void OnFinish(SailApprovalBusinessContext ctx);

    /// <summary>未到结束节点的终局（关闭/取消/作废/终止/撤回等）。</summary>
    void OnAborted(SailApprovalBusinessContext ctx);

    /// <summary>
    /// 办理前 Gate（Completing）。返回 null 或 Ok 通过；Reject 拦下；抛错 FailFast。
    /// </summary>
    HookResult? OnCompleting(FlowOperation operation, SailApprovalBusinessContext ctx);
}
