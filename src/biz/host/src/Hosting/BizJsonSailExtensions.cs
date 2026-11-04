using System;
using saiflow.biz.Json;
using saiflow.engine.Client;
using saiflow.engine.Hosting;

namespace saiflow.biz.Hosting;

/// <summary>
/// Biz composition：挂载默认 Newtonsoft <see cref="IJsonSerializer"/>。
/// </summary>
public static class BizJsonSailExtensions
{
    /// <summary>
    /// 注册 Newtonsoft 实现；之后仍可用 <c>UseJsonSerializer</c> 覆盖自定义实现。
    /// </summary>
    public static ISailCollection UseBizJson(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseJsonSerializer(new NewtonsoftJsonSerializer());
    }

    public static SailClientBuilder UseBizJson(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.UseBizJson();
        return builder;
    }
}
