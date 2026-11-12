namespace sailflow.assignee;

/// <summary>R10：时刻 A′ 有人后置过滤器（改人 / 打锚点）。禁止内嵌套 RunProcess。</summary>
public interface IAssigneeResolveFilter
{
    void Apply(AssigneeFilterContext ctx);
}
