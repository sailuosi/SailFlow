using sailflow.engine.Event;

namespace sailflow.engine.Hooks
{
    /// <summary>Gate / Hook / Notifier 注册过滤与模式选项。</summary>
    public sealed class HookRegistrationOptions
    {
        public HookMatch Match { get; set; } = HookMatch.Whitelist;
        public string[] ProcessCodes { get; set; }
        public string[] ProcessIds { get; set; }
        public string[] ActivityCodes { get; set; }
        public EventFireTypeEnum[] Events { get; set; }
        public TaskNotifyKind[] TaskKinds { get; set; }
        /// <summary>抄送 Notifier 专用；未配则不推送。</summary>
        public CopytoNotifyKind[] CopytoKinds { get; set; }
        /// <summary>
        /// 执行模式；默认 <see cref="HookMode.AsyncOutbox"/>。
        /// 设为 <see cref="HookMode.Sync"/> 前必读该枚举上的同事务自锁警示：须用 <c>ctx.Kit</c>，禁止另开引擎连接。
        /// </summary>
        public HookMode Mode { get; set; } = HookMode.AsyncOutbox;
        public int Order { get; set; }

        /// <summary>创建 Hook 扩展：按实例 BizChannel 过滤；空=不限。</summary>
        public string[] BizChannels { get; set; }

        /// <summary>创建 Hook 扩展：按实例 BizType 过滤；空=不限。</summary>
        public string[] BizTypes { get; set; }

        /// <summary>
        /// Notifier 专用：Outbox Drain 认领后的进程内限流。null 表示直调 <c>NotifyAsync</c>。
        /// Gate / Hook 忽略此字段。
        /// </summary>
        public NotifyThrottleOptions InProcessThrottle { get; set; }
    }
}
