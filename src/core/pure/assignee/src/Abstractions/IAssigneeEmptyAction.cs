namespace sailflow.assignee;

/// <summary>
/// R10：空集处置（选型，非 Filter 长链）。
/// Key 由节点策略码选择（如 reject / skip / admin / users）；默认 reject 对齐 R4。
/// </summary>
public interface IAssigneeEmptyAction
{
    string Key { get; }

    EmptyActionResult Handle(AssigneeFilterContext ctx);
}
