using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Hooks;

namespace saiflow.msg.Notify
{
    /// <summary>待办推送通道骨架：模板方法 + 默认失败隔离（不向外抛）。</summary>
    public abstract class TaskNotifyChannelBase : ITaskNotifier
    {
        public abstract string Name { get; }

        public async Task NotifyAsync(TaskNotifyContext ctx, CancellationToken ct)
        {
            if (ctx == null)
                return;

            try
            {
                OnBeforeSend(ctx);
                await SendCoreAsync(ctx, ct).ConfigureAwait(false);
                OnSucceeded(ctx);
            }
            catch (Exception ex)
            {
                OnFailed(ctx, ex);
            }
        }

        protected abstract Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct);

        protected virtual void OnBeforeSend(TaskNotifyContext ctx) { }

        protected virtual void OnSucceeded(TaskNotifyContext ctx) { }

        protected virtual void OnFailed(TaskNotifyContext ctx, Exception ex) =>
            Trace.TraceWarning($"[saiflow.msg] Notifier '{Name}' failed for task {ctx.TaskId}: {ex.Message}");
    }
}
