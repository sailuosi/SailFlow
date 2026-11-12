using System.Diagnostics;
using sailflow.engine.Common;

namespace sailflow.assignee.Builtin;

/// <summary>按 UserId（OrdinalIgnoreCase）去重，保留首次出现。不改 Source=Specific 时的语义以外字段。</summary>
public class DedupUsersFilter : IAssigneeResolveFilter
{
    public virtual void Apply(AssigneeFilterContext ctx)
    {
        if (ctx.Performers == null || ctx.Performers.Count <= 1)
            return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var next = new PerformerList();
        var removed = 0;
        foreach (var p in ctx.Performers)
        {
            var id = p?.UserId?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                next.Add(p!);
                continue;
            }

            if (!seen.Add(id))
            {
                removed++;
                continue;
            }

            next.Add(p!);
        }

        if (removed == 0)
            return;

        if (string.Equals(ctx.Source, PerformerSources.Specific, StringComparison.OrdinalIgnoreCase))
            ctx.RewroteSpecific = true;

        ctx.Performers = next;
        Trace.TraceInformation(
            "[Assignee] DedupUsersFilter removed {0} duplicate(s); Source={1} RewroteSpecific={2}",
            removed, ctx.Source, ctx.RewroteSpecific);
    }
}
