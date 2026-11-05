using System;
using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Hooks;

namespace saiflow.msg.SideEffect
{
    /// <summary>
    /// BestEffort 流程 Hook 骨架：主路径 <see cref="Handle"/> 只 <c>TryWrite</c> 入 Channel，
    /// 后台单消费者串行调用 <see cref="ConsumeAsync"/>（适合写文件等忌并发资源）。
    /// <para>
    /// 注册时必须 <c>Mode = Sync</c>（仅入队，禁止 AsyncOutbox——那是 Reliable 档）。
    /// 见 <see cref="BestEffortHookRegistration"/>。
    /// </para>
    /// </summary>
    public abstract class BestEffortFlowHookBase<TPayload> : IFlowHook, IDisposable
    {
        private readonly BestEffortChannelPump<TPayload> _pump;

        protected BestEffortFlowHookBase(BestEffortChannelOptions options = null)
        {
            _pump = new BestEffortChannelPump<TPayload>(options ?? new BestEffortChannelOptions(), ConsumeAsync);
            _pump.Start();
        }

        public abstract string Name { get; }

        /// <summary>主路径：映射 payload 后非阻塞入队；失败/满队静默丢弃。</summary>
        public void Handle(FlowHookContext ctx)
        {
            if (ctx == null)
                return;

            try
            {
                if (!TryCreatePayload(ctx, out var payload) || payload == null)
                    return;

                _ = _pump.TryWrite(payload);
            }
            catch (Exception)
            {
                // BestEffort：生产端异常不影响流转
            }
        }

        /// <summary>从上下文构造消费载荷；返回 false 表示跳过。</summary>
        protected abstract bool TryCreatePayload(FlowHookContext ctx, out TPayload payload);

        /// <summary>串行消费（单消费者）；业务写文件 / 打点等。异常由泵吞掉并打 Trace。</summary>
        protected abstract Task ConsumeAsync(TPayload payload, CancellationToken cancellationToken);

        public void Dispose() => _pump.Dispose();
    }
}
