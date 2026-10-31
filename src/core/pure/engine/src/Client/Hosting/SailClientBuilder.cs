using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using mooSQL.data;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// Fluent builder for <see cref="SailClient"/> (类比 WebApplicationBuilder)。
/// </summary>
public sealed class SailClientBuilder
{
    public ISailCollection Services { get; } = new SailCollection();

    public IConfiguration? Configuration { get; private set; }

    /// <summary>尚未 Build 的数据库工厂；也可由 UseDatabase 写入 Properties。</summary>
    public SailDBFactory? DbFactory { get; set; }

    /// <summary>可选：Build 前指定的缓存；未设则默认 <see cref="MemorySooCache"/>。</summary>
    public ISooCache? Cache { get; set; }

    public SailClientBuilder UseConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
        return this;
    }

    public SailClientBuilder UseDatabase(SailDBFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        DbFactory = factory;
        Services.UseDatabase(factory);
        return this;
    }

    public SailClientBuilder UseDatabase(mooSQL.data.DBInstance db, int mainPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(db);
        return UseDatabase(new DelegateSailDBFactory(db, mainPosition));
    }

    /// <summary>
    /// 注册缓存（对齐 mooSQL <c>builder.useCache(...)</c> / <c>client.useCache(...)</c>）。
    /// 默认不调用时，Build/EnsurePorts 会挂 <see cref="MemorySooCache"/>。
    /// </summary>
    public SailClientBuilder UseCache(ISooCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        Cache = cache;
        Services.UseCache(cache);
        return this;
    }

    public SailClientBuilder ConfigureServices(Action<ISailCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Services);
        return this;
    }

    /// <summary>积极构建：得到 <see cref="SailClient"/>，由宿主自行持有并注入工作类。</summary>
    public SailClient Build()
    {
        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.ConfigureServices))
        {
            d.ConfigureServices?.Invoke(Services);
        }

        var dbFactory = DbFactory;
        if (dbFactory == null
            && Services.Properties.TryGetValue(SailBuilderKeys.DbFactory, out var box))
        {
            dbFactory = box as SailDBFactory;
        }

        if (dbFactory == null)
        {
            throw new InvalidOperationException(
                "SailDBFactory is required. Call builder.UseDatabase(...) or Services.UseDatabase(...) before Build().");
        }

        var client = SailClientAssembler.Assemble(dbFactory, Configuration);

        var cache = Cache;
        if (cache == null
            && Services.Properties.TryGetValue(SailBuilderKeys.Cache, out var cacheBox))
        {
            cache = cacheBox as ISooCache;
        }

        if (cache != null)
            client.useCache(cache);
        // else：EnsurePorts / Cache getter 会创建默认 MemorySooCache

        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.ConfigureClient))
        {
            d.ConfigureClient?.Invoke(client, Services);
        }

        foreach (var d in Services.OrderBy(x => x.Order)
                     .Where(x => x.Phase == SailFeaturePhase.PostBuild))
        {
            d.PostBuild?.Invoke(client, Services);
        }

        return client;
    }
}
