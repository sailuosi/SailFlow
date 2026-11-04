using System;
using mooSQL.data;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

/// <summary>
/// 缓存注册（对齐 mooSQL <c>useCache(ISooCache)</c>）。
/// </summary>
public static class SailCacheExtensions
{
    /// <summary>在 Builder 服务集合上注册缓存；Build 时挂到 <see cref="SailClient"/>。</summary>
    public static ISailCollection UseCache(this ISailCollection services, ISooCache cache)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cache);
        services.Properties[SailBuilderKeys.Cache] = cache;
        return services;
    }
}
