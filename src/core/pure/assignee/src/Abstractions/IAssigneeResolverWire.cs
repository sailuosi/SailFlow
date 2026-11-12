namespace sailflow.assignee;

/// <summary>
/// 可选：管道 <c>WireResolver</c> 时注入 Sail / Registry / Callbacks。
/// 新 Builtin 实现本接口即可，无需再改 <see cref="AssigneePipeline"/> 类型开关。
/// </summary>
public interface IAssigneeResolverWire
{
    void Wire(AssigneePipeline pipeline);
}
