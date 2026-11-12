using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Hooks;

namespace sailflow.msg.Notify
{
    /// <summary>待办推送通道骨架：模板方法 + 默认失败隔离（不向外抛）。</summary>
    public abstract class TaskNotifyChannelBase : ITaskNotifier
    {
        /// <summary>
        /// 可选：推送失败时业务侧记录（在 OnFailed 之后调用）。
        /// Outbox 死信（<c>HookOutboxStatus.Dead</c>）由 Worker 单独治理，与此回调无关；需 Outbox 重试请设 <see cref="RethrowFailures"/>。
        /// </summary>
        public ITaskNotifyFailureHandler FailureHandler { get; set; }

        public abstract string Name { get; }

        /// <summary>为 true 时失败会重新抛出，供 Outbox 等路径走 MarkFailed；默认 false（邮件等通道吞异常）。</summary>
        protected virtual bool RethrowFailures => false;

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
                FailureHandler?.OnNotifyFailed(ctx, ex, Name);
                if (RethrowFailures)
                    throw;
            }
        }

        protected abstract Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct);

        protected virtual void OnBeforeSend(TaskNotifyContext ctx) { }

        protected virtual void OnSucceeded(TaskNotifyContext ctx) =>
            Trace.TraceInformation(
                $"[sailflow.msg] Notifier '{Name}' succeeded kind={ctx.Kind} process={ctx.ProcessCode} task={ctx.TaskId} intent={GetIntent(ctx)}");

        protected virtual void OnFailed(TaskNotifyContext ctx, Exception ex) =>
            Trace.TraceWarning(
                $"[sailflow.msg] Notifier '{Name}' failed kind={ctx.Kind} process={ctx.ProcessCode} task={ctx.TaskId} intent={GetIntent(ctx)}: {ex.Message}");

        protected static TaskNotifyMessageIntent GetIntent(TaskNotifyContext ctx) =>
            ctx == null ? TaskNotifyMessageIntent.Open : TaskNotifyMessageIntentMapping.FromKind(ctx.Kind);
    }
}
