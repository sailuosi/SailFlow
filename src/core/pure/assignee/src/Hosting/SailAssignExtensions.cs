using sailflow.assignee.Adapt;
using sailflow.assignee.Builtin;
using sailflow.engine.Assignee;
using sailflow.engine.Client;
using sailflow.engine.Hosting;

namespace sailflow.assignee.Hosting;

/// <summary>
/// R10：<c>UseAssign</c> 总包（Startup 心智）。<see cref="UseAssignees"/> 为其无配置别名。
/// </summary>
public static class SailAssignExtensions
{
    const string PendingBuildersKey = "sailflow.assignee.assign.builders";

    /// <summary>
    /// 启用办理人管道，并在子 Builder 上 <c>AddResolver&lt;T&gt;</c> / <c>AddFilter</c> / <c>AddEmptyAction</c>。
    /// </summary>
    public static SailClientBuilder UseAssign(
        this SailClientBuilder builder,
        Action<AssigneeAssignBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.UseAssign(configure);
        return builder;
    }

    public static ISailCollection UseAssign(
        this ISailCollection services,
        Action<AssigneeAssignBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var ab = new AssigneeAssignBuilder();
        configure?.Invoke(ab);
        GetPending(services).Add(ab);

        if (services.Any(f => string.Equals(f.Name, SailAssigneeExtensions.FeatureName, StringComparison.Ordinal)))
            return services;

        services.AddFeature(new SailFeatureDescriptor
        {
            Name = SailAssigneeExtensions.FeatureName,
            Order = 40,
            Phase = SailFeaturePhase.ConfigureClient,
            ConfigureClient = (client, svc) =>
            {
                client.UseAssignees();
                if (svc.Properties.TryGetValue(PendingBuildersKey, out var box)
                    && box is List<AssigneeAssignBuilder> builders)
                {
                    if (client.AssigneePipeline is not AssigneePipeline pipeline)
                        return;
                    foreach (var b in builders)
                        b.Apply(pipeline, client);
                }
            }
        });
        return services;
    }

    /// <summary>已 Build 的 Client：装内置后应用 configure。</summary>
    public static SailClient UseAssign(
        this SailClient client,
        Action<AssigneeAssignBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.UseAssignees();
        if (configure != null && client.AssigneePipeline is AssigneePipeline pipeline)
        {
            var ab = new AssigneeAssignBuilder();
            configure(ab);
            ab.Apply(pipeline, client);
        }

        return client;
    }

    static List<AssigneeAssignBuilder> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(PendingBuildersKey, out var box)
            && box is List<AssigneeAssignBuilder> list)
            return list;

        list = new List<AssigneeAssignBuilder>();
        services.Properties[PendingBuildersKey] = list;
        return list;
    }
}
