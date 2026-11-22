namespace sailflow.external.Approval;

/// <summary>采购申请样例 Status（示意；产品自定字典）。</summary>
public static class PurchaseStatusCodes
{
    public const string FormCode = "purchase";

    public const int Draft = 0;
    public const int Running = 1;
    public const int BackToModify = 2;
    public const int Approved = 3;
    public const int Rejected = 4;
    public const int Aborted = 5;
}
