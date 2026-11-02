using System;
using saiflow.biz.Bootstrap;
using saiflow.engine.Client.Hosting;

namespace saiflow.biz.Hosting;

public static class SchemaSailExtensions
{
    public static ISailCollection UseSchema(
        this ISailCollection services,
        Action<SchemaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var opt = new SchemaOptions();
        configure?.Invoke(opt);

        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = SailFeatureNames.Schema,
            Phase = SailFeaturePhase.PostBuild,
            Order = 900,
            DependsOn = new[] { SailFeatureNames.Database },
            PostBuild = (client, _) =>
            {
                SailSchemaInitializer.EnsureTables(client, includeAi: opt.IncludeAi);
                if (opt.Seed)
                    SailSchemaInitializer.EnsureMinimalSeed(client);
            }
        });
    }
}
