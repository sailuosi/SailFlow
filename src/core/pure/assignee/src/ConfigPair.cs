namespace sailflow.assignee;

/// <summary>配置对：Type（Scope）+ selectModel（Mode）+ Params。禁止用 enum 作分发键。</summary>
public class ConfigPair
{
    /// <summary>人员范围 Scope 注册键（BPMN @type）。</summary>
    public string Type { get; set; } = "";

    /// <summary>操作模式 Fixed|Pick|Preselect（BPMN @selectModel）。</summary>
    public string SelectModel { get; set; } = "";

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

    public static ConfigPair Create(
        string type,
        string? selectModel,
        IDictionary<string, object?>? parameters = null)
    {
        var pair = Create(type, parameters);
        pair.SelectModel = selectModel?.Trim() ?? "";
        return pair;
    }
}
