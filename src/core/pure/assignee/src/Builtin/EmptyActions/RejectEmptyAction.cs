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

/// <summary>
/// EmptyAction 选型键约定。
/// 管道空人顺序：已注册 EmptyAction(Key) → 对应 Resolver（同名 Type；
/// <see cref="Users"/>/<c>3</c>→FixedUsers）→ 默认 <see cref="Reject"/>。
/// </summary>
public static class EmptyActionKeys
{
    public const string Reject = "reject";
    /// <summary>与 NodePolicy assignEmptyHandlerType=2 对齐。</summary>
    public const string RejectProductCode = "2";
    public const string Skip = "skip";
    public const string SkipProductCode = "1";
    /// <summary>指定人员；无 EmptyAction 时映射 <c>FixedUsers</c> Resolver。</summary>
    public const string Users = "users";
    public const string UsersProductCode = "3";
    /// <summary>转管理员；需注册 EmptyAction 或自定义 Resolver Type，无内置映射。</summary>
    public const string Admin = "admin";
    public const string AdminProductCode = "4";
}
