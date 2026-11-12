namespace sailflow.assignee;

/// <summary>配置对：Type（string）+ Params。禁止用 enum 作分发键。</summary>
public class ConfigPair
{
    public string Type { get; set; } = "";

    public IDictionary<string, object?> Params { get; set; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public static ConfigPair Create(string type, IDictionary<string, object?>? parameters = null) =>
        new()
        {
            Type = type ?? "",
            Params = parameters != null
                ? new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };
}
