using sailflow.engine.Common;

namespace sailflow.assignee;

/// <summary>
/// 实例级预选办理人仓（按流程实例 + 活动定义 Id）。
/// 表与 SQL 在宿主具化；Resolver 只读 <see cref="Load"/>。
/// </summary>
public interface IPreselectAssigneeStore
{
    /// <summary>读取某节点预选办理人；无数据返回空列表（勿返回 null）。</summary>
    PerformerList Load(string processInstanceId, string activityId);

    /// <summary>整节点覆盖写入（先清后插）。</summary>
    void Replace(string processInstanceId, string activityId, PerformerList performers);

    /// <summary>清除某节点预选。</summary>
    void Clear(string processInstanceId, string activityId);
}
