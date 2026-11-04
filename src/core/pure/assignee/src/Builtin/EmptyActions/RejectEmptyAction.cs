using saiflow.engine.Assignee;

namespace saiflow.assignee.Builtin;

/// <summary>默认空人：拒绝推进（R4）。Key=<c>reject</c>，亦匹配产品码 <c>2</c>。</summary>
public class RejectEmptyAction : IAssigneeEmptyAction
{
    public virtual string Key => EmptyActionKeys.Reject;

    public virtual EmptyActionResult Handle(AssigneeFilterContext ctx) =>
        EmptyActionResult.Reject(
            AssigneeReasonCodes.AssigneeEmpty,
            "Assignee resolve empty; RejectEmptyAction (R4). Activity="
            + (ctx.Request.ActivityId ?? "(null)"));
}

/// <summary>EmptyAction 选型键约定。</summary>
public static class EmptyActionKeys
{
    public const string Reject = "reject";
    /// <summary>与 NodePolicy assignEmptyHandlerType=2 对齐。</summary>
    public const string RejectProductCode = "2";
    public const string Skip = "skip";
    public const string SkipProductCode = "1";
    public const string Users = "users";
    public const string UsersProductCode = "3";
    public const string Admin = "admin";
    public const string AdminProductCode = "4";
}
