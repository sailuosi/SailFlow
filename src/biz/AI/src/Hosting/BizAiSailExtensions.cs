using System;
using saiflow.AI.Runtime;
using saiflow.engine.Client.Hosting;

namespace saiflow.AI.Hosting;

public static class BizAiSailExtensions
{
    public static ISailCollection UseBizAI(
        this ISailCollection services,
        Action<BizAiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var opt = new BizAiOptions();
        configure?.Invoke(opt);

        if (opt.RegisterExecutor)
        {
            services.AddFeature(new SailFeatureDescriptor
            {
                Name = "BizAI",
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 200,
                ConfigureClient = (client, _) => AiRuntime.UseDefaultExecutor(client)
            });
        }

        if (opt.EnsureTables)
        {
            services.AddFeature(new SailFeatureDescriptor
            {
                Name = "BizAI.Schema",
                Phase = SailFeaturePhase.PostBuild,
                Order = 920,
                PostBuild = (client, _) => AiRuntime.EnsureTables(client.UseRunSQL())
            });
        }

        return services;
    }
}
