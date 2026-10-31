using saiflow.assignee.Adapt;
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
        WireBpmnConfigReaders(pipeline, client);
        client.useAssigneePipeline(pipeline);
        return client;
    }

    /// <summary>挂上 BPMN ConfigPair 读取（显式 sf:assigneeResolve / sf:leaveJudge 优先）。</summary>
    public static void WireBpmnConfigReaders(AssigneePipeline pipeline, SailClient client)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(client);
        pipeline.ResolveConfigReader = req => BpmnAssigneeConfigReader.ReadResolve(client, req);
        pipeline.LeaveConfigReader = req => BpmnAssigneeConfigReader.ReadLeave(client, req);
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
        // S4 扩展型（可继承 / 业务同码覆盖）
        registry.Register(new UnitMapAssigneeResolver());
        registry.Register(new JobRelativeAssigneeResolver());
        registry.Register(new BusinessCallbackAssigneeResolver());
        registry.Register(new ExpressionAssigneeResolver());
        registry.Register(new FallbackAssigneeResolver());
        registry.Register(new RaceLeaveJudge());
        registry.Register(new CollaborateLeaveJudge());
        registry.Register(new GroupRaceLeaveJudge());
        registry.Register(new GroupCollaborateLeaveJudge());
        registry.Register(new CaptainLeaveJudge());
    }

    /// <summary>注册业务回调（BusinessCallback.Params.callback）。</summary>
    public static SailClient useAssigneeBusinessCallback(
        this SailClient client,
        IAssigneeBusinessCallback callback)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(callback);
        EnsurePipeline(client).Callbacks.Register(callback);
        return client;
    }

    public static SailClient useAssigneeBusinessCallback(
        this SailClient client,
        string name,
        Func<AssigneeResolveContext, PerformerList> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        EnsurePipeline(client).Callbacks.Register(name, resolve);
        return client;
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
        WireBpmnConfigReaders(pipeline, client);
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
