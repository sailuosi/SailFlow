namespace sailflow.engine.NextStep;

/// <summary>演算后候选处置。</summary>
public interface INextStepFilter
{
    bool IsMatch(NextStepRouteContext ctx);

    void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates);
}
