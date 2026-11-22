using sailflow.engine.Hooks;

namespace sailflow.biz.Approval;

/// <summary>可选空实现，业务 Handler 按需 override。</summary>
public abstract class SailApprovalBusinessBase : ISailApprovalBusiness
{
    public abstract string FormCode { get; }

    public virtual void OnInit(SailApprovalBusinessContext ctx) { }
    public virtual void OnQuery(SailApprovalBusinessContext ctx) { }
    public virtual void OnSubmit(SailApprovalBusinessContext ctx) { }
    public virtual void OnStarted(SailApprovalBusinessContext ctx) { }
    public virtual void OnConsent(SailApprovalBusinessContext ctx) { }
    public virtual void OnBackToModify(SailApprovalBusinessContext ctx) { }
    public virtual void OnCancel(SailApprovalBusinessContext ctx) { }
    public virtual void OnFinish(SailApprovalBusinessContext ctx) { }
    public virtual void OnAborted(SailApprovalBusinessContext ctx) { }

    public virtual HookResult? OnCompleting(FlowOperation operation, SailApprovalBusinessContext ctx)
        => null;
}
