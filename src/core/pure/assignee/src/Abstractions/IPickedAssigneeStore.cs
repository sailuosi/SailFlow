using sailflow.utils;

namespace sailflow.assignee;

/// <summary>
/// Mode=Pick 且 <c>persistPick=true</c> 时的自选落库仓（与 Preselect 分表）。
/// 默认实现不落库：见 <see cref="NoopPickedAssigneeStore"/>。
/// API 名 <c>LoadPicked</c> / <c>StorePicked</c> 与 Fixed Scope.Resolve 拆开。
/// </summary>
public interface IPickedAssigneeStore
{
    /// <summary>读取某节点已持久化的自选办理人；无数据返回空列表（勿返回 null）。</summary>
    PerformerList LoadPicked(string processInstanceId, string activityId);

    /// <summary>整节点覆盖写入自选办理人（先清后插）。</summary>
    void StorePicked(string processInstanceId, string activityId, PerformerList performers);

    /// <summary>清除某节点自选落库。</summary>
    void ClearPicked(string processInstanceId, string activityId);
}

/// <summary>
/// 默认不落库实现：Load 恒空；Store/Clear 空操作。
/// 宿主显式开 persist 且挂真实表实现前，管道仅吃当次入参。
/// </summary>
public sealed class NoopPickedAssigneeStore : IPickedAssigneeStore
{
    public static NoopPickedAssigneeStore Instance { get; } = new();

    public PerformerList LoadPicked(string processInstanceId, string activityId) => new();

    public void StorePicked(string processInstanceId, string activityId, PerformerList performers)
    {
        // 默认实现不落库
    }

    public void ClearPicked(string processInstanceId, string activityId)
    {
        // 默认实现不落库
    }
}
