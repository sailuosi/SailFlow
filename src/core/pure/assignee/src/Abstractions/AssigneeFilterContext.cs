using saiflow.engine.Assignee;
using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>R10：Resolve 后置 Filter / EmptyAction 上下文。</summary>
public sealed class AssigneeFilterContext
{
    public required AssigneeInvokeRequest Request { get; init; }

    /// <summary><see cref="PerformerSources"/> 常量。</summary>
    public string Source { get; set; } = PerformerSources.Unknown;

    /// <summary>Resolve Type（若有）。</summary>
    public string? ResolveType { get; set; }

    public PerformerList Performers { get; set; } = new();

    /// <summary>自动审批意图（只写不执行）。</summary>
    public List<AutoApproveAnchor> Anchors { get; } = new();

    /// <summary>诊断：是否改写过 Specific 来源列表。</summary>
    public bool RewroteSpecific { get; set; }

    /// <summary>可选只读策略袋（如 NodePolicy JSON）；由宿主/Filter 约定。</summary>
    public IDictionary<string, object?>? PolicyBag { get; set; }
}
