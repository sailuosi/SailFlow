using System;
using saiflow.engine.Event;
using saiflow.engine.Hooks;

namespace saiflow.msg.SideEffect
{
    /// <summary>BestEffort Hook 注册约定：强制 Sync（只入队）。</summary>
    public static class BestEffortHookRegistration
    {
        /// <summary>
        /// 应用到 <see cref="HookRegistrationOptions"/>：<c>Mode=Sync</c>，并设置事件列表。
        /// </summary>
        public static void Apply(HookRegistrationOptions options, params EventFireTypeEnum[] events)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.Mode = HookMode.Sync;
            if (events != null && events.Length > 0)
                options.Events = events;
        }

        public static Action<HookRegistrationOptions> Configure(
            HookMatch match = HookMatch.All,
            int order = 0,
            params EventFireTypeEnum[] events)
        {
            return o =>
            {
                Apply(o, events);
                o.Match = match;
                o.Order = order;
            };
        }
    }
}
