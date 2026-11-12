using System.Globalization;

namespace sailflow.assignee;

/// <summary>ConfigPair.Params 读取辅助。</summary>
public static class ConfigPairParams
{
    public static string? GetString(ConfigPair config, string key, string? defaultValue = null)
    {
        if (config?.Params == null || string.IsNullOrEmpty(key))
            return defaultValue;
        if (!config.Params.TryGetValue(key, out var raw) || raw == null)
            return defaultValue;
        return Convert.ToString(raw, CultureInfo.InvariantCulture) ?? defaultValue;
    }

    public static bool GetBool(ConfigPair config, string key, bool defaultValue = false)
    {
        var s = GetString(config, key);
        if (string.IsNullOrEmpty(s))
            return defaultValue;
        return bool.TryParse(s, out var b) ? b : defaultValue;
    }

    public static float? GetFloat(ConfigPair config, string key)
    {
        var s = GetString(config, key);
        if (string.IsNullOrEmpty(s))
            return null;
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : null;
    }

    public static int? GetInt(ConfigPair config, string key)
    {
        var s = GetString(config, key);
        if (string.IsNullOrEmpty(s))
            return null;
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;
    }

    public static string[] GetStringArray(ConfigPair config, string key)
    {
        if (config?.Params == null || string.IsNullOrEmpty(key))
            return Array.Empty<string>();
        if (!config.Params.TryGetValue(key, out var raw) || raw == null)
            return Array.Empty<string>();

        if (raw is string[] arr)
            return arr.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (raw is IEnumerable<object> objs)
            return objs.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .ToArray();
        if (raw is string s)
            return s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        return Array.Empty<string>();
    }
}
