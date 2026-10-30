using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

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
