using System;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// Notifier Outbox 消费侧进程内限流（Drain 认领之后、真正 <c>NotifyAsync</c> 之前）。
    /// 有界 Channel 背压 + <see cref="MaxDegree"/> 并发 + 可选 <see cref="MinInterval"/> 削峰。
    /// 成功/失败仍等真实调用结束后再 MarkSucceeded / MarkFailed（禁止入队即结案）。
    /// </summary>
    public sealed class NotifyThrottleOptions
    {
        public const int DefaultCapacity = 256;
        public const int DefaultMaxDegree = 1;

        /// <summary>有界 Channel 容量；满时 Drain 写入阻塞（背压），不丢 Outbox 行。</summary>
        public int Capacity { get; set; } = DefaultCapacity;

        /// <summary>同时执行 <c>NotifyAsync</c> 的路数（≥1）。</summary>
        public int MaxDegree { get; set; } = DefaultMaxDegree;

        /// <summary>两次开始执行之间的最小间隔（同 notifier 车道）；Zero 表示不额外等待。</summary>
        public TimeSpan MinInterval { get; set; } = TimeSpan.Zero;

        /// <summary>规范化：Capacity/MaxDegree 至少为 1；负间隔视为 Zero。</summary>
        public NotifyThrottleOptions Normalized()
        {
            return new NotifyThrottleOptions
            {
                Capacity = Math.Max(1, Capacity),
                MaxDegree = Math.Max(1, MaxDegree),
                MinInterval = MinInterval < TimeSpan.Zero ? TimeSpan.Zero : MinInterval,
            };
        }
    }
}
