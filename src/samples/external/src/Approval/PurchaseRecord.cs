namespace sailflow.external.Approval;

/// <summary>采购申请样例单据（内存示意，非产品表）。</summary>
public sealed class PurchaseRecord
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public int Status { get; set; } = PurchaseStatusCodes.Draft;
    public string? ProcessInstanceId { get; set; }
    public string? ProgressText { get; set; }
}
