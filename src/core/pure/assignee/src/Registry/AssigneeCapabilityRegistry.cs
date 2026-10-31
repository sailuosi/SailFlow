namespace saiflow.assignee;

/// <summary>按 Type:string 注册；后写覆盖前者（业务可覆写内置）。</summary>
public class AssigneeCapabilityRegistry
{
    private readonly Dictionary<string, IAssigneeResolver> _resolvers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IActivityLeaveJudge> _judges =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAssigneeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        if (string.IsNullOrWhiteSpace(resolver.Type))
            throw new ArgumentException("IAssigneeResolver.Type is required.", nameof(resolver));
        _resolvers[resolver.Type] = resolver;
    }

    public void Register(IActivityLeaveJudge judge)
    {
        ArgumentNullException.ThrowIfNull(judge);
        if (string.IsNullOrWhiteSpace(judge.Type))
            throw new ArgumentException("IActivityLeaveJudge.Type is required.", nameof(judge));
        _judges[judge.Type] = judge;
    }

    public bool TryGetResolver(string type, out IAssigneeResolver? resolver)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            resolver = null;
            return false;
        }

        return _resolvers.TryGetValue(type, out resolver);
    }

    public bool TryGetLeaveJudge(string type, out IActivityLeaveJudge? judge)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            judge = null;
            return false;
        }

        return _judges.TryGetValue(type, out judge);
    }

    public IAssigneeResolver GetResolver(string type)
    {
        if (TryGetResolver(type, out var resolver) && resolver != null)
            return resolver;
        throw new InvalidOperationException($"No IAssigneeResolver registered for type '{type}'.");
    }

    public IActivityLeaveJudge GetLeaveJudge(string type)
    {
        if (TryGetLeaveJudge(type, out var judge) && judge != null)
            return judge;
        throw new InvalidOperationException($"No IActivityLeaveJudge registered for type '{type}'.");
    }

    public IReadOnlyCollection<string> ResolverTypes => _resolvers.Keys.ToList();
    public IReadOnlyCollection<string> LeaveJudgeTypes => _judges.Keys.ToList();
}
