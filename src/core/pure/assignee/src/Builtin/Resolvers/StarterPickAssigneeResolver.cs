using System;
using sailflow.utils;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 旧模式码 Type=<c>StarterPick</c>。新图用 selectModel=Preselect + allowPreselectFrom 含 Start。
/// <b>不再</b>进 <c>RegisterBuiltins</c>；保留类仅兼容外部引用。
/// </summary>
[Obsolete("Use selectModel=Preselect + allowPreselectFrom containing Start. Not registered as a Scope.")]
public class StarterPickAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.StarterPick;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx) =>
        PickInputPerformers.FromRequest(ctx);
}

/// <summary>
/// 旧模式码 Type=<c>ApproverPick</c>。新图用 selectModel=Pick。
/// <b>不再</b>进 <c>RegisterBuiltins</c>；Pick 入参读取请用 <see cref="PickInputPerformers"/>。
/// </summary>
[Obsolete("Use selectModel=Pick. Request performers: PickInputPerformers.FromRequest. Not registered as a Scope.")]
public class ApproverPickAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.ApproverPick;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx) =>
        PickInputPerformers.FromRequest(ctx);
}
