using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>发送前闸门：仅 Sync；用结果拒绝，不抛业务校验异常。</summary>
    public interface IFlowGate
    {
        string Name { get; }
        HookResult Validate(FlowHookContext ctx);
    }

    /// <summary>业务钩子：Sync 当场调；Async 由 Outbox Worker 调同一方法。</summary>
    public interface IFlowHook
    {
        string Name { get; }
        void Handle(FlowHookContext ctx);
    }

    /// <summary>待办旁路：默认异步 Outbox 后再调业务实现。</summary>
    public interface ITaskNotifier
    {
        string Name { get; }
        Task NotifyAsync(TaskNotifyContext ctx, CancellationToken ct);
    }

    /// <summary>抄送旁路：独立于待办 Notifier；默认异步 Outbox。</summary>
    public interface ICopytoNotifier
    {
        string Name { get; }
        Task NotifyAsync(CopytoNotifyContext ctx, CancellationToken ct);
    }
}
