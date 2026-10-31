namespace saiflow.engine.Client;

/// <summary>
/// JSON 序列化插槽（engine 只依赖本契约；Newtonsoft / STJ 等由宿主挂载）。
/// </summary>
public interface IJsonSerializer
{
    string Serialize(object value);

    T Deserialize<T>(string json);
}
