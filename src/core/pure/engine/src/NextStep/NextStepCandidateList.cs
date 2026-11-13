using System.Collections.Generic;

namespace sailflow.engine.NextStep;

public sealed class NextStepCandidateList
{
    public IList<NextStepCandidate> Items { get; } = new List<NextStepCandidate>();
}
