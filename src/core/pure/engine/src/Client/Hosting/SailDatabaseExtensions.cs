using System;
using mooSQL.data;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

public static class SailDatabaseExtensions
{
    public static ISailCollection UseDatabase(this ISailCollection services, SailDBFactory dbFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbFactory);
        services.Properties[SailBuilderKeys.DbFactory] = dbFactory;
        services.ProvideFeature(SailFeatureNames.Database, order: 0);
        return services;
    }

    public static ISailCollection UseDatabase(
        this ISailCollection services,
        DBInstance db,
        int mainPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(db);
        return services.UseDatabase(new DelegateSailDBFactory(db, mainPosition));
    }

    public static ISailCollection UseDatabase(
        this ISailCollection services,
        Func<int, DBInstance> getDb,
        int mainPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(getDb);
        return services.UseDatabase(new DelegateSailDBFactory(getDb, mainPosition));
    }
}
