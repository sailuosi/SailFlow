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
    }
}
