using System.Globalization;
using sailflow.engine.Common;

namespace sailflow.assignee.Builtin;

/// <summary>从 runner.Conditions / 扩展字典取字符串。</summary>
public static class ConditionBag
{
    public static string? Get(AssigneeResolveContext ctx, string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;
        var key = field.Trim();
        var conditions = ctx.Runner?.Conditions;
        if (conditions != null)
        {
            if (conditions.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                return v;
            foreach (var kv in conditions)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(kv.Value))
                    return kv.Value;
            }
        }

        // Params 里也可直接带静态 unit 值
        return ConfigPairParams.GetString(ctx.Config, key);
    }

    public static PerformerList ToPerformers(
        IEnumerable<string> userIds,
        string? groupId = null,
        string? groupRole = null,
        string? unitId = null,
        string? unitCode = null,
        string? unitName = null)
    {
        var list = new PerformerList();
        var role = string.IsNullOrWhiteSpace(groupRole) ? GroupRoles.Member : groupRole.Trim();
        foreach (var id in userIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            var uid = id.Trim();
            list.Add(new Performer(uid, uid)
            {
                GroupId = groupId,
                GroupRole = role,
                UnitId = unitId,
                UnitCode = unitCode,
                UnitName = unitName
            });
        }
        return list;
    }

    public static IEnumerable<string> SplitIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            yield break;
        foreach (var part in raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Length > 0)
                yield return t;
        }
    }

    public static IEnumerable<string> MapLookup(ConfigPair? config, string unitKey)
    {
        if (config?.Params == null || string.IsNullOrEmpty(unitKey))
            yield break;
        if (!config.Params.TryGetValue("maps", out var maps) || maps == null)
            yield break;

        if (maps is IDictionary<string, object?> dictObj)
        {
            if (TryDict(dictObj, unitKey, out var raw) && raw != null)
            {
                foreach (var id in ExpandRaw(raw))
                    yield return id;
            }
            yield break;
        }

        if (maps is IDictionary<string, object> dict)
        {
            if (TryDictObj(dict, unitKey, out var raw) && raw != null)
            {
                foreach (var id in ExpandRaw(raw))
                    yield return id;
            }
            yield break;
        }

        if (maps is System.Collections.IDictionary idict)
        {
            foreach (System.Collections.DictionaryEntry e in idict)
            {
                if (!string.Equals(Convert.ToString(e.Key), unitKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var id in ExpandRaw(e.Value))
                    yield return id;
                yield break;
            }
        }
    }

    private static bool TryDict(IDictionary<string, object?> dict, string key, out object? raw)
    {
        if (dict.TryGetValue(key, out raw))
            return true;
        foreach (var kv in dict)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                raw = kv.Value;
                return true;
            }
        }
        raw = null;
        return false;
    }

    private static bool TryDictObj(IDictionary<string, object> dict, string key, out object? raw)
    {
        if (dict.TryGetValue(key, out var v))
        {
            raw = v;
            return true;
        }
        foreach (var kv in dict)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                raw = kv.Value;
                return true;
            }
        }
        raw = null;
        return false;
    }

    private static IEnumerable<string> ExpandRaw(object? raw)
    {
        if (raw == null)
            yield break;
        if (raw is string s)
        {
            foreach (var id in SplitIds(s))
                yield return id;
            yield break;
        }
        if (raw is IEnumerable<object> objs)
        {
            foreach (var o in objs)
            {
                var id = Convert.ToString(o, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(id))
                    yield return id.Trim();
            }
            yield break;
        }
        var one = Convert.ToString(raw, CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(one))
            yield return one.Trim();
    }
}
