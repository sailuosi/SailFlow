namespace sailflow.assignee;

/// <summary>Filter 注册选项；未设 Order 时按注册顺序递增。</summary>
public sealed class AssigneeFilterRegistrationOptions
{
    /// <summary>执行序；同 Order 按注册先后。</summary>
    public int? Order { get; set; }
}
