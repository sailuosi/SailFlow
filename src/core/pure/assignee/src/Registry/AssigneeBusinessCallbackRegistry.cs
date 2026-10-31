using saiflow.engine.Common;

namespace saiflow.assignee;

/// <summary>业务回调注册表：后写覆盖同名 Name。</summary>
public class AssigneeBusinessCallbackRegistry
{
    private readonly Dictionary<string, IAssigneeBusinessCallback> _map =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAssigneeBusinessCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (string.IsNullOrWhiteSpace(callback.Name))
            throw new ArgumentException("Callback Name required.", nameof(callback));
        _map[callback.Name.Trim()] = callback;
    }

    public void Register(string name, Func<AssigneeResolveContext, PerformerList> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        Register(new DelegateCallback(name, resolve));
    }

    public bool TryGet(string name, out IAssigneeBusinessCallback? callback)
    {
        callback = null;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        return _map.TryGetValue(name.Trim(), out callback) && callback != null;
    }

    private sealed class DelegateCallback : IAssigneeBusinessCallback
    {
        private readonly Func<AssigneeResolveContext, PerformerList> _resolve;

        public DelegateCallback(string name, Func<AssigneeResolveContext, PerformerList> resolve)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _resolve = resolve;
        }

        public string Name { get; }

        public PerformerList Resolve(AssigneeResolveContext ctx) =>
            _resolve(ctx) ?? new PerformerList();
    }
}
