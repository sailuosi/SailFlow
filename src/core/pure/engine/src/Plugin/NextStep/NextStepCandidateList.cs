using System.Collections.Generic;

namespace sailflow.nextStep;
public sealed class NextStepCandidateList
{
    public IList<NextStepCandidate> Items { get; } = new List<NextStepCandidate>();
}
