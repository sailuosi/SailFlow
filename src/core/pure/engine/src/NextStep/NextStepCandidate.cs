using sailflow.engine.Xpdl;

namespace sailflow.engine.NextStep;

/// <summary>结构演算后的候选节点（可标注）。</summary>
public sealed class NextStepCandidate
{
    public string ActivityId { get; set; }
    public Activity Activity { get; set; }
    public bool Disabled { get; set; }
    public bool Required { get; set; }
    public string ReasonCode { get; set; }
}
