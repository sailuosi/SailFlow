using Newtonsoft.Json;
using saiflow.engine.Client;

namespace saiflow.biz.Json;

/// <summary>biz 默认 JSON 实现（Newtonsoft.Json）。</summary>
public sealed class NewtonsoftJsonSerializer : IJsonSerializer
{
    public string Serialize(object value) => JsonConvert.SerializeObject(value);

    public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;
}
