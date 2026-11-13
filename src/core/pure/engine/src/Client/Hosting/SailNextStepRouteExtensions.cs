using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Client;
using sailflow.engine.NextStep;

namespace sailflow.engine.Hosting;

/// <summary>下一跳节点计算管道：<c>UseNextStepRoute</c> 唯一门面。</summary>
public static class SailNextStepRouteExtensions
{
    public const string FeatureName = SailFeatureNames.NextStepRoute;
    const string PendingBuildersKey = "sailflow.engine.nextstep.builders";

    public static SailClientBuilder UseNextStepRoute(
        this SailClientBuilder builder,
        Action<NextStepRouteBuilder> configure = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        builder.Services.UseNextStepRoute(configure);
        return builder;
    }

    public static ISailCollection UseNextStepRoute(
        this ISailCollection services,
        Action<NextStepRouteBuilder> configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var rb = new NextStepRouteBuilder();
        configure?.Invoke(rb);
        GetPending(services).Add(rb);

        if (services.Any(f => string.Equals(f.Name, FeatureName, StringComparison.Ordinal)))
            return services;

        services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Order = 45,
            Phase = SailFeaturePhase.ConfigureClient,
            ConfigureClient = (client, svc) =>
            {
                EnsurePipeline(client);
                if (svc.Properties.TryGetValue(PendingBuildersKey, out var box)
                    && box is List<NextStepRouteBuilder> builders)
                {
                    if (client.NextStepRoute is not NextStepRoutePipeline pipeline)
                        return;
                    foreach (var b in builders)
                        b.Apply(pipeline, client);
                }
            }
        });
        return services;
    }

    /// <summary>已 Build 的 Client：挂管道并应用 configure。</summary>
    public static SailClient UseNextStepRoute(
        this SailClient client,
        Action<NextStepRouteBuilder> configure = null)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        var pipeline = EnsurePipeline(client);
        if (configure != null)
        {
            var rb = new NextStepRouteBuilder();
            configure(rb);
            rb.Apply(pipeline, client);
        }
        else
        {
            pipeline.EnsureDefaultLinqEvaluator();
        }

        return client;
    }

    static NextStepRoutePipeline EnsurePipeline(SailClient client)
    {
        if (client.NextStepRoute is NextStepRoutePipeline existing)
            return existing;
        var pipeline = new NextStepRoutePipeline();
        client.useNextStepRoute(pipeline);
        return pipeline;
    }

    static List<NextStepRouteBuilder> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(PendingBuildersKey, out var box)
            && box is List<NextStepRouteBuilder> list)
            return list;
        list = new List<NextStepRouteBuilder>();
        services.Properties[PendingBuildersKey] = list;
        return list;
    }
}
