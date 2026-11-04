using System.Globalization;
using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>
/// 缺省/兜底：先跑 primary ConfigPair，空结果再跑 fallback。
/// Params.primary / Params.fallback = { type, params }。禁止 sealed。
/// </summary>
public class FallbackAssigneeResolver : IAssigneeResolver
{
    public AssigneeCapabilityRegistry? Registry { get; set; }

    /// <summary>
    /// 嵌套 Resolver 注入（由 <see cref="AssigneePipeline"/> 挂 <c>WireSail</c>），
    /// 确保 RoleGroup / ProcessRelative / BusinessCallback 等拿到 Sail/Callbacks。
    /// </summary>
    public Action<IAssigneeResolver>? WireNested { get; set; }

    public virtual string Type => AssigneeResolveTypeCodes.Fallback;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var primary = ReadNested(ctx.Config, "primary");
        var list = TryResolve(ctx, primary);
        if (list.Count > 0)
            return list;

        var fallback = ReadNested(ctx.Config, "fallback");
        return TryResolve(ctx, fallback);
    }

    protected virtual PerformerList TryResolve(AssigneeResolveContext ctx, ConfigPair? nested)
    {
        if (nested == null || string.IsNullOrWhiteSpace(nested.Type) || Registry == null)
            return new PerformerList();
        if (!Registry.TryGetResolver(nested.Type, out var resolver) || resolver == null)
            return new PerformerList();
        // 避免 Fallback 自递归
        if (string.Equals(resolver.Type, Type, StringComparison.OrdinalIgnoreCase))
            return new PerformerList();

        WireNested?.Invoke(resolver);

        return resolver.Resolve(new AssigneeResolveContext
        {
            Request = ctx.Request,
            Config = nested
        }) ?? new PerformerList();
    }

    protected virtual ConfigPair? ReadNested(ConfigPair? parent, string key)
    {
        if (parent?.Params == null || !parent.Params.TryGetValue(key, out var raw) || raw == null)
            return null;

        if (raw is ConfigPair pair)
            return pair;

        if (raw is IDictionary<string, object?> d1)
            return FromDict(d1.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase));

        if (raw is IDictionary<string, object> d2)
            return FromDict(d2.ToDictionary(
                kv => kv.Key,
                kv => (object?)kv.Value,
                StringComparer.OrdinalIgnoreCase));

        if (raw is System.Collections.IDictionary idict)
        {
            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry e in idict)
            {
                var k = Convert.ToString(e.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(k))
                    map[k] = e.Value;
            }
            return FromDict(map);
        }

        if (raw is string json && !string.IsNullOrWhiteSpace(json))
        {
            var parsed = ConfigPairJson.ParseParams(json);
            // 若整段是 {"type":"...","params":{...}}
            if (parsed.TryGetValue("type", out var t) && t != null)
                return FromDict(parsed);
        }

        return null;
    }

    private static ConfigPair? FromDict(IDictionary<string, object?> map)
    {
        if (!map.TryGetValue("type", out var typeObj) || typeObj == null)
            return null;
        var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(type))
            return null;

        IDictionary<string, object?> parameters =
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (map.TryGetValue("params", out var p) && p != null)
        {
            if (p is IDictionary<string, object?> pd)
                parameters = new Dictionary<string, object?>(pd, StringComparer.OrdinalIgnoreCase);
            else if (p is IDictionary<string, object> po)
                parameters = po.ToDictionary(kv => kv.Key, kv => (object?)kv.Value,
                    StringComparer.OrdinalIgnoreCase);
            else if (p is string json)
                parameters = ConfigPairJson.ParseParams(json);
        }

        return ConfigPair.Create(type.Trim(), parameters);
    }
}
