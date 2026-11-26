using sailflow.utils;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 任意选人 Scope：不展开固定名单。Fixed 下会得到空集交 EmptyAction；
/// Pick 路径由管道直接读入参，通常不调用本 Resolver。
/// </summary>
public class AnyManAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.AnyMan;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx) => new();
}
