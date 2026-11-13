namespace sailflow.engine.NextStep;

/// <summary>演算前准备：写条件袋（Client 级 IfCondition）。</summary>
public interface INextStepPrepare
{
    /// <summary>Options 已命中后的第二道门；默认 true。</summary>
    bool IsMatch(NextStepRouteContext ctx);

    void Prepare(NextStepRouteContext ctx);
}
