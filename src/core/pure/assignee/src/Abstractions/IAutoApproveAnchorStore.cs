using mooSQL.data;
using saiflow.engine.Client;

namespace saiflow.assignee;

/// <summary>R10：自动审批锚点持久化（推荐与发送同库；可用 HookOutbox Kind=AssigneeAutoApprove）。</summary>
public interface IAutoApproveAnchorStore
{
    /// <summary>入队一批锚点。kit 非空时尽量走调用方事务。</summary>
    void Enqueue(SailClient sail, IReadOnlyList<AutoApproveAnchor> anchors, SQLBuilder? kit = null);
}
