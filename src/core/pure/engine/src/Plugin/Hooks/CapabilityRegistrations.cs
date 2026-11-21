using System;
using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    public sealed class GateRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public string[] ActivityCodes { get; init; }
        public IFlowGate Handler { get; init; }
    }

    public sealed class HookRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public string[] ActivityCodes { get; init; }
        public EventFireTypeEnum[] Events { get; init; }
        public HookMode Mode { get; init; }
        public IFlowHook Handler { get; init; }

        /// <summary>注册级超时覆盖；null 则走 Handler/<see cref="FlowCapabilityOptions"/>。</summary>
        public int? TimeoutMs { get; init; }
    }

    public sealed class NotifierRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public string[] ActivityCodes { get; init; }
        public TaskNotifyKind[] TaskKinds { get; init; }
        public HookMode Mode { get; init; }
        public ITaskNotifier Handler { get; init; }

        /// <summary>Outbox 消费侧进程内限流；null 则 Drain 直调 Handler。</summary>
        public NotifyThrottleOptions InProcessThrottle { get; init; }
    }

    public sealed class CopytoNotifierRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public CopytoNotifyKind[] CopytoKinds { get; init; }
        public HookMode Mode { get; init; }
        public ICopytoNotifier Handler { get; init; }
    }

    public sealed class ProcessInstanceCreateHookRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public string[] BizChannels { get; init; }
        public string[] BizTypes { get; init; }
        public IProcessInstanceCreateHook Handler { get; init; }

        /// <summary>注册级超时覆盖；null 则走 Handler/<see cref="FlowCapabilityOptions"/>。</summary>
        public int? TimeoutMs { get; init; }
    }

    internal sealed class DelegateFlowGate : IFlowGate
    {
        private readonly Func<FlowHookContext, HookResult> _validate;

        public DelegateFlowGate(string name, Func<FlowHookContext, HookResult> validate)
        {
            Name = name;
            _validate = validate ?? throw new ArgumentNullException(nameof(validate));
        }

        public string Name { get; }
        public HookResult Validate(FlowHookContext ctx) => _validate(ctx);
    }

    internal sealed class DelegateFlowHook : FlowHookBase
    {
        private readonly Action<FlowHookContext> _handle;

        public DelegateFlowHook(string name, Action<FlowHookContext> handle, int? timeoutMs = null)
        {
            Name = name;
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            if (timeoutMs.HasValue)
                TimeoutMs = timeoutMs.Value;
        }

        public override string Name { get; }
        public override void Handle(FlowHookContext ctx) => _handle(ctx);
    }

    internal sealed class DelegateTaskNotifier : ITaskNotifier
    {
        private readonly Func<TaskNotifyContext, CancellationToken, Task> _notify;

        public DelegateTaskNotifier(string name, Func<TaskNotifyContext, CancellationToken, Task> notify)
        {
            Name = name;
            _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        }

        public string Name { get; }
        public Task NotifyAsync(TaskNotifyContext ctx, CancellationToken ct) => _notify(ctx, ct);
    }

    internal sealed class DelegateCopytoNotifier : ICopytoNotifier
    {
        private readonly Func<CopytoNotifyContext, CancellationToken, Task> _notify;

        public DelegateCopytoNotifier(string name, Func<CopytoNotifyContext, CancellationToken, Task> notify)
        {
            Name = name;
            _notify = notify ?? throw new ArgumentNullException(nameof(notify));
        }

        public string Name { get; }
        public Task NotifyAsync(CopytoNotifyContext ctx, CancellationToken ct) => _notify(ctx, ct);
    }

    internal sealed class DelegateProcessInstanceCreateHook : ProcessInstanceCreateHookBase
    {
        private readonly Action<ProcessInstanceCreateContext> _handle;

        public DelegateProcessInstanceCreateHook(string name, Action<ProcessInstanceCreateContext> handle, int? timeoutMs = null)
        {
            Name = name;
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            if (timeoutMs.HasValue)
                TimeoutMs = timeoutMs.Value;
        }

        public override string Name { get; }
        public override void OnCreating(ProcessInstanceCreateContext ctx) => _handle(ctx);
    }
}
