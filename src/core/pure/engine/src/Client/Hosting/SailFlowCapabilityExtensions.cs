using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Hooks;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// Gate / Hook / Notifier 三轨注册扩展。
/// </summary>
/// <remarks>
/// <para>
/// <b>命名约定：</b>
/// <c>UseXxx</c> = 启用一项能力包（总包 / 开关）；
/// <c>AddXxx</c> = 可多次追加的注册项（同名后覆盖）。
/// </para>
/// <para>
/// <b>产品语义（刻意拆开，不可混成一个接口）：</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="IFlowGate"/> — 流转真正落库推进前的同步校验；
/// 仅 Sync；可 <see cref="HookResult.Reject"/> 并映射到 <c>WfExecutedResult</c>（业务拒绝，不当事故异常）。
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="IFlowHook"/> — 校验通过并推进后的业务扩展（回写、汇总、结束出单等）；
/// 可选 Sync（同事务 FailFast）或 AsyncOutbox（入 <c>wf_hook_outbox</c> 由 Drain 消费）。
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ITaskNotifier"/> — 待办创建/完成/转交等通知旁路（邮件 / IM / HTTP）；
/// 强制 AsyncOutbox，禁止拖慢主路径；多通道按 <c>Order</c> 串行且失败隔离。
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>主路径顺序：</b>
/// Gate（OnValidating）→ 内核写库推进 → Sync Hook → Async Hook/Notifier 写 Outbox → 返回结果；
/// 后台 Drain 认领 Outbox 再调 Handler。
/// </para>
/// <para>
/// <b>双轨 API：</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// 构建中（推荐）：<c>builder.UseFlowAspects</c> 总包 + <c>AddGate</c> / <c>AddHook</c> / <c>AddNotifier</c> —
/// 写入 <see cref="ISailCollection.Properties"/> 的 pending 列表（键 <see cref="SailBuilderKeys.FlowCapabilities"/>），
/// 由 <see cref="SailClientBuilder.Build"/> 调用 <see cref="ApplyPendingRegistrations"/> 落到
/// <see cref="SailClient.Capabilities"/>。
/// </description>
/// </item>
/// <item>
/// <description>
/// 构建后补挂：小写 <c>client.addGate</c> / <c>addHook</c> / <c>addNotifier</c> —
/// 直接注册到已组装的 Client（测试或 PostBuild 热补挂）。
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>总包与分轨：</b>
/// <see cref="UseFlowAspects"/> 仅收拢写法（回调参数即 builder/services 自身）；
/// 内层调 <c>AddGate</c> / <c>AddHook</c> / <c>AddNotifier</c>（实例、委托或无参泛型）。
/// Outbox Drain 见独立扩展 <c>UseHookOutboxDrain</c>（本类不负责启动后台循环）。
/// </para>
/// <para>
/// <b>注册过滤要点（<see cref="HookRegistrationOptions"/>）：</b>
/// 默认 <c>Match = Whitelist</c>，须配 <c>ProcessCodes</c> / <c>ProcessIds</c> 否则永不命中；
/// 全局用 <c>Match = All</c>。Hook 未配 <c>Events</c>、Notifier 未配 <c>TaskKinds</c> 亦不会执行。
/// Gate 事件固定 OnValidating（可不必填 Events）；Gate 忽略 Mode（恒 Sync）。
/// 同名后注册覆盖先前并打 Warning；多通道须不同 <c>Name</c>。
/// </para>
/// <para>
/// <b>与其它机制边界：</b>
/// Gate ≠ 待办人 Assignee 管道；Notifier ≠ 抄送；
/// 不替代 BPMN Action/Service/Script（图内编排仍走节点扩展）。
/// 按需开启：不调用本扩展时主路径不跑业务回调；默认邮件须显式 <c>UseDefaultTaskMail()</c>。
/// </para>
/// <para>
/// 设计全文：<c>doc/sail/特性设计-回调与事件.md</c>；
/// 使用教程：<c>doc/helps/教程-事件体系Gate-Hook-Notifier-Outbox.md</c>；
/// 宿主能力包约定：<c>doc/sail/最佳实践-宿主能力包UseXxx.md</c>。
/// </para>
/// </remarks>
public static class SailFlowCapabilityExtensions
{
    /// <summary>
    /// 总包入口：在同一回调内收拢注册 Gate / Hook / Notifier（及可选的 Outbox Drain 调用）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="configure">配置委托；参数即 <paramref name="services"/> 自身，内层继续链式调用分轨 API。</param>
    /// <returns>同一 <paramref name="services"/>，便于继续链式。</returns>
    /// <remarks>
    /// 本方法本身不注册任何能力，只提供作用域写法，避免宿主 <c>Program</c> 散落多段
    /// <c>AddGate</c>/<c>AddHook</c>/<c>AddNotifier</c>。
    /// 示例：
    /// <code>
    /// services.UseFlowAspects(s =>
    /// {
    ///     // 样例类型在 samples/external（sailflow.external.Hooks）
    ///     s.AddGate&lt;FinancePreSendGate&gt;(o => o.ProcessCodes = new[] { "EXPENSE" });
    ///     s.AddHook(new FinanceAggregateHook(), o =>
    ///     {
    ///         o.ProcessCodes = new[] { "EXPENSE" };
    ///         o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
    ///     });
    ///     // SampleHttpTaskNotifier → samples/external（sailflow.external.Notify）
    ///     s.AddNotifier(new SampleHttpTaskNotifier(url), o =>
    ///     {
    ///         o.Match = HookMatch.All;
    ///         o.TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed };
    ///     });
    /// });
    /// </code>
    /// </remarks>
    public static ISailCollection UseFlowAspects(
        this ISailCollection services,
        Action<ISailCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        return services;
    }

    /// <summary>
    /// 总包入口（Builder 轨）：在同一回调内收拢注册 Gate / Hook / Notifier。
    /// </summary>
    /// <param name="builder">SailClient 构建器。</param>
    /// <param name="configure">配置委托；参数即 <paramref name="builder"/> 自身。</param>
    /// <returns>同一 <paramref name="builder"/>，便于继续链式。</returns>
    /// <remarks>语义同 <see cref="UseFlowAspects(ISailCollection, Action{ISailCollection})"/>。</remarks>
    public static SailClientBuilder UseFlowAspects(
        this SailClientBuilder builder,
        Action<SailClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder);
        return builder;
    }

    /// <summary>
    /// 追加无参构造 Gate（语法糖，等价 <c>AddGate(new TGate(), configure)</c>）。
    /// </summary>
    /// <typeparam name="TGate">须实现 <see cref="IFlowGate"/> 且具备无参构造；有构造期依赖时请改用 <see cref="AddGate(ISailCollection, IFlowGate, Action{HookRegistrationOptions})"/>。</typeparam>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="configure">可选：白名单、ActivityCodes、<c>Order</c> 等（Gate 忽略 Mode）。</param>
    public static ISailCollection AddGate<TGate>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where TGate : class, IFlowGate, new()
        => services.AddGate(new TGate(), configure);

    /// <summary>
    /// 追加无参构造 Hook（语法糖，等价 <c>AddHook(new THook(), configure)</c>）。
    /// </summary>
    /// <typeparam name="THook">须实现 <see cref="IFlowHook"/> 且具备无参构造。</typeparam>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="configure">可选：须声明 <c>Events</c>；默认 Mode 为 AsyncOutbox；Sync 仅用于短、可 FailFast 的同事务逻辑。</param>
    public static ISailCollection AddHook<THook>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where THook : class, IFlowHook, new()
        => services.AddHook(new THook(), configure);

    /// <summary>
    /// 追加无参构造 Notifier（语法糖，等价 <c>AddNotifier(new TNotifier(), configure)</c>）。
    /// </summary>
    /// <typeparam name="TNotifier">须实现 <see cref="ITaskNotifier"/> 且具备无参构造；有参构造（如 HTTP URL）请用 <see cref="AddNotifier(ISailCollection, ITaskNotifier, Action{HookRegistrationOptions})"/>。</typeparam>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="configure">可选：须声明 <c>TaskKinds</c>；强制 AsyncOutbox。</param>
    public static ISailCollection AddNotifier<TNotifier>(
        this ISailCollection services,
        Action<HookRegistrationOptions> configure = null)
        where TNotifier : class, ITaskNotifier, new()
        => services.AddNotifier(new TNotifier(), configure);

    /// <summary>
    /// Builder 轨：追加无参构造 Gate（转调 <see cref="AddGate{TGate}(ISailCollection, Action{HookRegistrationOptions})"/>）。
    /// </summary>
    public static SailClientBuilder AddGate<TGate>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where TGate : class, IFlowGate, new()
    {
        builder.Services.AddGate<TGate>(configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：追加无参构造 Hook（转调 <see cref="AddHook{THook}(ISailCollection, Action{HookRegistrationOptions})"/>）。
    /// </summary>
    public static SailClientBuilder AddHook<THook>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where THook : class, IFlowHook, new()
    {
        builder.Services.AddHook<THook>(configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：追加无参构造 Notifier（转调 <see cref="AddNotifier{TNotifier}(ISailCollection, Action{HookRegistrationOptions})"/>）。
    /// </summary>
    public static SailClientBuilder AddNotifier<TNotifier>(
        this SailClientBuilder builder,
        Action<HookRegistrationOptions> configure = null)
        where TNotifier : class, ITaskNotifier, new()
    {
        builder.Services.AddNotifier<TNotifier>(configure);
        return builder;
    }

    /// <summary>
    /// 追加流程 Gate（推进落库前同步校验；可多次）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="gate">Gate 实例；<c>Name</c> 用于同名覆盖判定与日志。</param>
    /// <param name="configure">可选过滤：默认 Whitelist，须配 ProcessCodes/ProcessIds；节点级请配 ActivityCodes；Order 控制多 Gate 顺序。</param>
    /// <returns>同一 <paramref name="services"/>。</returns>
    /// <remarks>
    /// <para>
    /// 写入 pending，Build 时 <see cref="FlowCapabilityRegistry.RegisterGate"/>；
    /// 并 <c>ProvideFeature(FlowGate, order: 30)</c> 供依赖校验。
    /// </para>
    /// <para>
    /// Reject 经 <c>WfExecutedResult.ApplyGateReject</c>：Status=Failed，带 Message / ValidationCode / ValidationDetails；
    /// 前端可用 <c>IsValidationReject()</c> 区分业务拒绝与引擎事故。禁止在 Gate 内写流程变量或打慢外部网关。
    /// </para>
    /// </remarks>
    public static ISailCollection AddGate(
        this ISailCollection services,
        IFlowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(gate);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterGate(gate, options));
        services.ProvideFeature(SailFeatureNames.FlowGate, order: 30);
        return services;
    }

    /// <summary>
    /// 以委托形式追加 Gate（临时/短逻辑，不必单独建 class）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="name">注册名（同名后覆盖）。</param>
    /// <param name="validate">同步校验委托，返回 <see cref="HookResult.Ok"/> 或 <see cref="HookResult.Reject"/>。</param>
    /// <param name="configure">可选过滤选项。</param>
    /// <remarks>内部包装为 <c>DelegateFlowGate</c> 再转调实例重载。</remarks>
    public static ISailCollection AddGate(
        this ISailCollection services,
        string name,
        Func<FlowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return services.AddGate(new DelegateFlowGate(name, validate), configure);
    }

    /// <summary>
    /// 追加流程 Hook（推进后业务扩展；可多次）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="hook">Hook 实例。</param>
    /// <param name="configure">
    /// 必填倾向：声明 <c>Events</c>（未配则永不执行）；
    /// Mode 默认 AsyncOutbox；Sync 仅 FailFast（异常/超时回滚主事务），禁止 Sync+弱依赖推送。
    /// 仅 Sync Hook 允许写流程变量；Async 禁止写变量。
    /// </param>
    /// <returns>同一 <paramref name="services"/>。</returns>
    /// <remarks>
    /// pending + <c>ProvideFeature(FlowHook, order: 31)</c>。
    /// 推送类场景请用 <see cref="AddNotifier(ISailCollection, ITaskNotifier, Action{HookRegistrationOptions})"/>，避免被误配成 Sync。
    /// </remarks>
    public static ISailCollection AddHook(
        this ISailCollection services,
        IFlowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hook);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterHook(hook, options));
        services.ProvideFeature(SailFeatureNames.FlowHook, order: 31);
        return services;
    }

    /// <summary>
    /// 以委托形式追加 Hook（临时/短逻辑）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="name">注册名。</param>
    /// <param name="handle">同步处理委托（AsyncOutbox 时由 Drain 侧再调；委托体仍为同步签名）。</param>
    /// <param name="configure">须声明 Events；按需设 Mode / Match / Order。</param>
    public static ISailCollection AddHook(
        this ISailCollection services,
        string name,
        Action<FlowHookContext> handle,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return services.AddHook(new DelegateFlowHook(name, handle), configure);
    }

    /// <summary>
    /// 追加待办 Notifier（旁路通知；默认入 Outbox；可多次）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="notifier">Notifier 实例；多通道须不同 <c>Name</c>，按 Order 串行，一通道失败不取消后续。</param>
    /// <param name="configure">
    /// 须声明 <c>TaskKinds</c>（Created/Completed/Entrusted 等）；未配则不推送。
    /// Mode 强制 AsyncOutbox（配 Sync 也会被改回）；可选 <c>InProcessThrottle</c> 做进程内限流。
    /// </param>
    /// <returns>同一 <paramref name="services"/>。</returns>
    /// <remarks>
    /// pending + <c>ProvideFeature(TaskNotifier, order: 32)</c>。
    /// 默认 SMTP 邮件请用 msg 包 <c>UseDefaultTaskMail()</c>（内部亦调本方法）；
    /// 生产可靠投递需同时启用 <c>UseHookOutboxDrain</c>。
    /// Notifier ≠ 抄送；抄送应独立通道，勿塞进待办推送。
    /// </remarks>
    public static ISailCollection AddNotifier(
        this ISailCollection services,
        ITaskNotifier notifier,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(notifier);
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        GetPending(services).Add(r => r.RegisterNotifier(notifier, options));
        services.ProvideFeature(SailFeatureNames.TaskNotifier, order: 32);
        return services;
    }

    /// <summary>
    /// 以委托形式追加 Notifier（临时/短逻辑）。
    /// </summary>
    /// <param name="services">引擎服务集合。</param>
    /// <param name="name">注册名。</param>
    /// <param name="notify">异步通知委托；失败应自管日志/重试语义（或依赖 Outbox 重试）。</param>
    /// <param name="configure">须声明 TaskKinds。</param>
    public static ISailCollection AddNotifier(
        this ISailCollection services,
        string name,
        Func<TaskNotifyContext, CancellationToken, Task> notify,
        Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(notify);
        return services.AddNotifier(new DelegateTaskNotifier(name, notify), configure);
    }

    /// <summary>
    /// Builder 轨：追加 Gate 实例（转调 Collection 重载）。
    /// </summary>
    public static SailClientBuilder AddGate(
        this SailClientBuilder builder,
        IFlowGate gate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddGate(gate, configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：委托形式追加 Gate。
    /// </summary>
    public static SailClientBuilder AddGate(
        this SailClientBuilder builder,
        string name,
        Func<FlowHookContext, HookResult> validate,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddGate(name, validate, configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：追加 Hook 实例。
    /// </summary>
    public static SailClientBuilder AddHook(
        this SailClientBuilder builder,
        IFlowHook hook,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddHook(hook, configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：委托形式追加 Hook。
    /// </summary>
    public static SailClientBuilder AddHook(
        this SailClientBuilder builder,
        string name,
        Action<FlowHookContext> handle,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddHook(name, handle, configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：追加 Notifier 实例。
    /// </summary>
    public static SailClientBuilder AddNotifier(
        this SailClientBuilder builder,
        ITaskNotifier notifier,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddNotifier(notifier, configure);
        return builder;
    }

    /// <summary>
    /// Builder 轨：委托形式追加 Notifier。
    /// </summary>
    public static SailClientBuilder AddNotifier(
        this SailClientBuilder builder,
        string name,
        Func<TaskNotifyContext, CancellationToken, Task> notify,
        Action<HookRegistrationOptions> configure = null)
    {
        builder.Services.AddNotifier(name, notify, configure);
        return builder;
    }

    /// <summary>
    /// 构建后补挂 Gate：直接写入已组装 <see cref="SailClient.Capabilities"/>（不经 pending）。
    /// </summary>
    /// <remarks>
    /// 命名小写以区分 Builder 期 <c>AddGate</c>。
    /// 宿主能力包应优先在 Builder 声明；本方法适用于测试、PostBuild 内 <c>new Xxx(client)</c> 后再注册等场景。
    /// </remarks>
    public static SailClient addGate(this SailClient client, IFlowGate gate, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterGate(gate, ApplyOptions(configure));
        return client;
    }

    /// <summary>
    /// 构建后补挂 Hook（直接注册到 Client）。
    /// </summary>
    public static SailClient addHook(this SailClient client, IFlowHook hook, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterHook(hook, ApplyOptions(configure));
        return client;
    }

    /// <summary>
    /// 构建后补挂 Notifier（直接注册到 Client）。
    /// </summary>
    public static SailClient addNotifier(this SailClient client, ITaskNotifier notifier, Action<HookRegistrationOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.Capabilities.RegisterNotifier(notifier, ApplyOptions(configure));
        return client;
    }

    /// <summary>
    /// 取得（或创建）Builder 期 pending 动作列表，键为 <see cref="SailBuilderKeys.FlowCapabilities"/>。
    /// </summary>
    /// <remarks>
    /// 每项为 <c>Action&lt;FlowCapabilityRegistry&gt;</c>，在 Build 时对 <see cref="SailClient.Capabilities"/> 执行，
    /// 从而把 Gate/Hook/Notifier 从声明期推迟到 Client 已组装之后落地。
    /// </remarks>
    internal static List<Action<FlowCapabilityRegistry>> GetPending(ISailCollection services)
    {
        if (services.Properties.TryGetValue(SailBuilderKeys.FlowCapabilities, out var box)
            && box is List<Action<FlowCapabilityRegistry>> list)
            return list;

        list = new List<Action<FlowCapabilityRegistry>>();
        services.Properties[SailBuilderKeys.FlowCapabilities] = list;
        return list;
    }

    /// <summary>
    /// 将 pending 注册应用到 Client（由 <see cref="SailClientBuilder.Build"/> 调用）。
    /// </summary>
    /// <remarks>
    /// 生命周期落点：Assemble SailClient 之后、ConfigureClient / PostBuild 前后以 Builder 实现为准；
    /// 无 pending 时静默返回。宿主勿在 Build 前假定 Capabilities 已含这些注册。
    /// </remarks>
    internal static void ApplyPendingRegistrations(SailClient client, ISailCollection services)
    {
        if (!services.Properties.TryGetValue(SailBuilderKeys.FlowCapabilities, out var box)
            || box is not List<Action<FlowCapabilityRegistry>> pending)
            return;

        foreach (var action in pending)
            action(client.Capabilities);
    }

    /// <summary>将可选 configure 落到新的 <see cref="HookRegistrationOptions"/>（Client 轨即时注册用）。</summary>
    private static HookRegistrationOptions ApplyOptions(Action<HookRegistrationOptions> configure)
    {
        var options = new HookRegistrationOptions();
        configure?.Invoke(options);
        return options;
    }
}
