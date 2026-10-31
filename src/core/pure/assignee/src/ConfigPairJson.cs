using System.Text.Json;

namespace saiflow.assignee;

/// <summary>ConfigPair.Params JSON 解析（BPMN @params 属性）。</summary>
public static class ConfigPairJson
{
    public static IDictionary<string, object?> ParseParams(string? json)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return dict;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return dict;

            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = ToClr(prop.Value);
        }
        catch (JsonException)
        {
            // 非法 JSON：交空 Params，仍保留 Type
        }

        return dict;
    }

    public static ConfigPair? FromTypeAndParamsJson(string? type, string? paramsJson)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;
        return ConfigPair.Create(type.Trim(), ParseParams(paramsJson));
    }

    private static object? ToClr(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
            JsonValueKind.Null => null,
            JsonValueKind.Array => el.EnumerateArray().Select(ToClr).ToArray(),
            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(p => p.Name, p => ToClr(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => el.ToString()
        };
}
