using saiflow.assignee.Builtin;
using saiflow.engine.Assignee;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using saiflow.engine.Common;

namespace saiflow.assignee.Hosting;

/// <summary>挂载办理人管道与内置 Type（后注册可覆盖）。</summary>
public static class SailAssigneeExtensions
{
    public const string FeatureName = "assignee";

    /// <summary>创建默认 Registry、注册内置、注入 <see cref="IAssigneePipeline"/>。</summary>
    public static SailClient UseAssignees(this SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var registry = new AssigneeCapabilityRegistry();
        RegisterBuiltins(registry);
        var pipeline = new AssigneePipeline(registry) { Sail = client };
        client.useAssigneePipeline(pipeline);
        return client;
    }

    public static SailClient useAssigneeResolver(this SailClient client, IAssigneeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(resolver);
        var pipeline = EnsurePipeline(client);
        pipeline.Registry.Register(resolver);
        return client;
    }

    public static SailClient useAssigneeResolver(
        this SailClient client,
        string type,
        Func<AssigneeResolveContext, PerformerList> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        return client.useAssigneeResolver(new DelegateAssigneeResolver(type, resolve));
    }

    public static SailClient useLeaveJudge(this SailClient client, IActivityLeaveJudge judge)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(judge);
        var pipeline = EnsurePipeline(client);
        pipeline.Registry.Register(judge);
        return client;
    }

    public static SailClient useLeaveJudge(
        this SailClient client,
        string type,
        Func<LeaveJudgeContext, LeaveJudgeResult> canLeave)
    {
        ArgumentNullException.ThrowIfNull(canLeave);
        return client.useLeaveJudge(new DelegateLeaveJudge(type, canLeave));
    }

    public static ISailCollection UseAssignees(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Order = 40,
            Phase = SailFeaturePhase.ConfigureClient,
            ConfigureClient = (client, _) => client.UseAssignees()
        });
        return services;
    }

    public static SailClientBuilder UseAssignees(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.UseAssignees();
        return builder;
    }

    public static void RegisterBuiltins(AssigneeCapabilityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register(new SpecificAssigneeResolver());
        registry.Register(new FixedUsersAssigneeResolver());
        registry.Register(new RoleGroupAssigneeResolver());
        registry.Register(new ProcessRelativeAssigneeResolver());
        registry.Register(new StarterPickAssigneeResolver());
        registry.Register(new ApproverPickAssigneeResolver());
        registry.Register(new RaceLeaveJudge());
        registry.Register(new CollaborateLeaveJudge());
    }

    private static AssigneePipeline EnsurePipeline(SailClient client)
    {
        if (client.AssigneePipeline is AssigneePipeline pipeline)
        {
            pipeline.Sail ??= client;
            return pipeline;
        }

        var registry = new AssigneeCapabilityRegistry();
        RegisterBuiltins(registry);
        pipeline = new AssigneePipeline(registry) { Sail = client };
        client.useAssigneePipeline(pipeline);
        return pipeline;
    }

    private sealed class DelegateAssigneeResolver : IAssigneeResolver
    {
        private readonly Func<AssigneeResolveContext, PerformerList> _resolve;

        public DelegateAssigneeResolver(string type, Func<AssigneeResolveContext, PerformerList> resolve)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _resolve = resolve;
        }

        public string Type { get; }

        public PerformerList Resolve(AssigneeResolveContext ctx) => _resolve(ctx);
    }

    private sealed class DelegateLeaveJudge : IActivityLeaveJudge
    {
        private readonly Func<LeaveJudgeContext, LeaveJudgeResult> _canLeave;

        public DelegateLeaveJudge(string type, Func<LeaveJudgeContext, LeaveJudgeResult> canLeave)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _canLeave = canLeave;
        }

        public string Type { get; }

        public LeaveJudgeResult CanLeave(LeaveJudgeContext ctx) => _canLeave(ctx);
    }
}
