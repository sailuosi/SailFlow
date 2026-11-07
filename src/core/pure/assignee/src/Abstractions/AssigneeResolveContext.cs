using saiflow.engine.Assignee;
using saiflow.engine.Common;

namespace saiflow.assignee;

public class AssigneeResolveContext
{
    public AssigneeInvokeRequest Request { get; set; } = null!;
    public ConfigPair Config { get; set; } = null!;

    public string ActivityId => Request.ActivityId;
    public FlowCommandContext? Runner => Request.Runner;
    public IDictionary<string, PerformerList>? NextActivityPerformers => Request.NextActivityPerformers;
}
