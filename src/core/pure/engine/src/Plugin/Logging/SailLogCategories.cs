namespace sailflow.engine.Logging
{
    /// <summary>运行时日志 category 约定（便于过滤）。</summary>
    public static class SailLogCategories
    {
        public const string SyncHook = "sync-hook";
        public const string AsyncHook = "async-hook";
        public const string Gate = "gate";
        public const string Notifier = "notifier";
        public const string CreateHook = "create-hook";
        public const string Assignee = "assignee";
        public const string Outbox = "outbox";

        /// <summary>流转族动作（Start/Run/Reject…）。</summary>
        public const string Transition = "transition";

        /// <summary>办理人变化族（Transfer/Delegate…）。</summary>
        public const string AssigneeAction = "assignee-action";

        /// <summary>实例生命周期族（Cancel/Suspend…）。</summary>
        public const string Lifecycle = "lifecycle";

        /// <summary>沟通等待等硬锁。</summary>
        public const string Guard = "guard";

        /// <summary>Manager / Runtime 等通用引擎失败。</summary>
        public const string Engine = "engine";

        /// <summary>
        /// 控制台块标题：<c>插件 · 流程回调 · {Title} · 异步</c>。
        /// 无插件 Title 时返回 null（由 Sink 回退到级别·category）。
        /// </summary>
        public static string PluginBlockTitle(string category, string pluginTitle)
        {
            if (string.IsNullOrWhiteSpace(pluginTitle))
                return null;

            var title = pluginTitle.Trim();
            var kind = KindLabel(category);
            var mode = ModeLabel(category);
            if (string.IsNullOrEmpty(kind))
                return string.IsNullOrEmpty(mode)
                    ? "插件 · " + title
                    : "插件 · " + title + " · " + mode;
            if (string.IsNullOrEmpty(mode))
                return "插件 · " + kind + " · " + title;
            return "插件 · " + kind + " · " + title + " · " + mode;
        }

        /// <summary>category → 中文类型（流程回调 / 门禁 / 通知…）。</summary>
        public static string KindLabel(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return null;
            switch (category.Trim())
            {
                case SyncHook:
                case AsyncHook:
                    return "流程回调";
                case CreateHook:
                    return "实例创建";
                case Gate:
                    return "门禁";
                case Notifier:
                    return "通知";
                case Outbox:
                    return "发件箱";
                case Assignee:
                    return "办理人";
                case Transition:
                    return "流转";
                case AssigneeAction:
                    return "办理人动作";
                case Lifecycle:
                    return "生命周期";
                case Guard:
                    return "硬锁";
                case Engine:
                    return "引擎";
                default:
                    return category.Trim();
            }
        }

        /// <summary>category → 同步 / 异步；门禁等无模式时返回 null。</summary>
        public static string ModeLabel(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return null;
            switch (category.Trim())
            {
                case SyncHook:
                case CreateHook:
                case Gate:
                    return "同步";
                case AsyncHook:
                case Notifier:
                case Outbox:
                    return "异步";
                default:
                    return null;
            }
        }
    }
}
