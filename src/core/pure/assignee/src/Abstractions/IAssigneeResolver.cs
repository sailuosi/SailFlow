using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>时刻 A：按 Type 查找待办人。内置实现禁止 sealed。</summary>
public interface IAssigneeResolver
{
    /// <summary>与 ConfigPair.Type 对应的字符串码。</summary>
    string Type { get; }

    PerformerList Resolve(AssigneeResolveContext ctx);
}
