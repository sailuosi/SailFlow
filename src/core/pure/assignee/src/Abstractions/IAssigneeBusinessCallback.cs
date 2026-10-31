using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>
/// 业务动态取人回调（场景 9）。按 name 注册到 <see cref="AssigneeBusinessCallbackRegistry"/>。
/// </summary>
public interface IAssigneeBusinessCallback
{
    /// <summary>回调名，与 ConfigPair.Params.callback 相等（OrdinalIgnoreCase）。</summary>
    string Name { get; }

    PerformerList Resolve(AssigneeResolveContext ctx);
}
