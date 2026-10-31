using System;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// JSON 序列化（<see cref="IJsonSerializer"/>）注册。
/// </summary>
public static class SailJsonSerializerExtensions
{
    /// <summary>登记一个 <see cref="IJsonSerializer"/>；Build 时挂到 <see cref="SailClient"/>。</summary>
    public static ISailCollection UseJsonSerializer(this ISailCollection services, IJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serializer);
        services.Properties[SailBuilderKeys.JsonSerializer] = serializer;
        return services;
    }
}
