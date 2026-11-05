using saiflow.msg.Notify;

namespace saiflow.msg.SideEffect
{
    /// <summary>
    /// Reliable 待办推送骨架：走引擎 AsyncOutbox；失败重新抛出供 Outbox 重试 / 死信。
    /// 业务只实现 <see cref="TaskNotifyChannelBase.SendCoreAsync"/>；须配合 <c>UseHookOutboxDrain</c>。
    /// <para>
    /// 禁止在 <c>SendCoreAsync</c> 内再丢 Channel 后立刻返回成功（Outbox 会提前 Succeeded）。
    /// 限流应做在 Drain 认领之后或外部客户端侧，且须等真实调用完成后再返回。
    /// </para>
    /// </summary>
    public abstract class ReliableTaskNotifierBase : TaskNotifyChannelBase
    {
        /// <summary>Reliable：失败须抛出，Outbox 才能 MarkFailed / 退避。</summary>
        protected override bool RethrowFailures => true;
    }
}
