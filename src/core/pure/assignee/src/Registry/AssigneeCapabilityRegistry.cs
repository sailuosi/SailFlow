using System.Diagnostics;

namespace saiflow.assignee;

/// <summary>按 Type:string 注册；后写覆盖前者（业务可覆写内置）。R9：同码覆盖强制可观测。</summary>
public class AssigneeCapabilityRegistry
{
    private readonly Dictionary<string, IAssigneeResolver> _resolvers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IActivityLeaveJudge> _judges =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _overriddenResolvers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _overriddenJudges =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _resolverImplNames =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _judgeImplNames =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAssigneeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        if (string.IsNullOrWhiteSpace(resolver.Type))
            throw new ArgumentException("IAssigneeResolver.Type is required.", nameof(resolver));

        var type = resolver.Type.Trim();
        var impl = resolver.GetType().FullName ?? resolver.GetType().Name;
        if (_resolvers.TryGetValue(type, out var prev) && prev != null)
        {
            _overriddenResolvers.Add(type);
            var prevName = prev.GetType().FullName ?? prev.GetType().Name;
            Trace.TraceWarning(
                "[Assignee] Resolve Type '{0}' overridden: {1} -> {2}",
                type, prevName, impl);
            Debug.WriteLine($"[Assignee] Resolve Type '{type}' overridden: {prevName} -> {impl}");
        }

        _resolvers[type] = resolver;
        _resolverImplNames[type] = impl;
    }

    public void Register(IActivityLeaveJudge judge)
    {
        ArgumentNullException.ThrowIfNull(judge);
        if (string.IsNullOrWhiteSpace(judge.Type))
            throw new ArgumentException("IActivityLeaveJudge.Type is required.", nameof(judge));

        var type = judge.Type.Trim();
        var impl = judge.GetType().FullName ?? judge.GetType().Name;
        if (_judges.TryGetValue(type, out var prev) && prev != null)
        {
            _overriddenJudges.Add(type);
            var prevName = prev.GetType().FullName ?? prev.GetType().Name;
            Trace.TraceWarning(
                "[Assignee] Leave Type '{0}' overridden: {1} -> {2}",
                type, prevName, impl);
            Debug.WriteLine($"[Assignee] Leave Type '{type}' overridden: {prevName} -> {impl}");
        }

        _judges[type] = judge;
        _judgeImplNames[type] = impl;
    }

    public bool IsResolverOverridden(string type) =>
        !string.IsNullOrWhiteSpace(type) && _overriddenResolvers.Contains(type);

    public bool IsLeaveJudgeOverridden(string type) =>
        !string.IsNullOrWhiteSpace(type) && _overriddenJudges.Contains(type);

    public string? GetResolverImplementationName(string type) =>
        _resolverImplNames.TryGetValue(type ?? "", out var n) ? n : null;

    public string? GetLeaveJudgeImplementationName(string type) =>
        _judgeImplNames.TryGetValue(type ?? "", out var n) ? n : null;

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
