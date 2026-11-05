using saiflow.engine.Event;

namespace saiflow.engine.Hooks
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
        public HookMode Mode { get; set; } = HookMode.AsyncOutbox;
        public int Order { get; set; }

        /// <summary>
        /// Notifier 专用：Outbox Drain 认领后的进程内限流。null 表示直调 <c>NotifyAsync</c>。
        /// Gate / Hook 忽略此字段。
        /// </summary>
        public NotifyThrottleOptions InProcessThrottle { get; set; }
    }
}
