using System;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Event;

namespace saiflow.engine.Hooks
{
    public sealed class GateRegistration
    {
        public string Name { get; init; }
        public int Order { get; init; }
        public HookMatch Match { get; init; }
        public string[] ProcessCodes { get; init; }
        public string[] ProcessIds { get; init; }
        public string[] ActivityCodes { get; init; }
        public IWorkflowGate Handler { get; init; }
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
        public IWorkflowHook Handler { get; init; }
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
    }

    internal sealed class DelegateWorkflowGate : IWorkflowGate
    {
        private readonly Func<WorkflowHookContext, HookResult> _validate;

        public DelegateWorkflowGate(string name, Func<WorkflowHookContext, HookResult> validate)
        {
            Name = name;
            _validate = validate ?? throw new ArgumentNullException(nameof(validate));
        }

        public string Name { get; }
        public HookResult Validate(WorkflowHookContext ctx) => _validate(ctx);
    }

    internal sealed class DelegateWorkflowHook : IWorkflowHook
    {
        private readonly Action<WorkflowHookContext> _handle;

        public DelegateWorkflowHook(string name, Action<WorkflowHookContext> handle)
        {
            Name = name;
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public string Name { get; }
        public void Handle(WorkflowHookContext ctx) => _handle(ctx);
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
}
