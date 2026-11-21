namespace sailflow.engine.Hooks
{
    /// <summary>插件 Name / Title 日志展示。</summary>
    public static class CapabilityPluginNames
    {
        /// <summary>
        /// 日志用展示名：有中文 Title 时为 <c>name「Title」</c>，否则仅 name。
        /// </summary>
        public static string Display(string name, string title)
        {
            var n = string.IsNullOrWhiteSpace(name) ? "-" : name.Trim();
            if (string.IsNullOrWhiteSpace(title))
                return n;
            return n + "「" + title.Trim() + "」";
        }

        /// <summary>注册 Title 优先，否则取 Handler 上的 Title。</summary>
        public static string ResolveTitle(string registrationTitle, string handlerTitle) =>
            !string.IsNullOrWhiteSpace(registrationTitle)
                ? registrationTitle.Trim()
                : (string.IsNullOrWhiteSpace(handlerTitle) ? null : handlerTitle.Trim());
    }
}
