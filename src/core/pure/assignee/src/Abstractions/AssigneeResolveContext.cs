using sailflow.engine.Assignee;
using sailflow.engine.Common;

namespace sailflow.assignee;

public class AssigneeResolveContext
{
    public AssigneeInvokeRequest Request { get; set; } = null!;
    public ConfigPair Config { get; set; } = null!;

    public string ActivityId => Request.ActivityId;
    public FlowCommandContext? Runner => Request.Runner;
    public IDictionary<string, PerformerList>? NextActivityPerformers => Request.NextActivityPerformers;
}
