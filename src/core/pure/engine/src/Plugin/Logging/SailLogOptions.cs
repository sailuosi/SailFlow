namespace sailflow.engine.Logging
{
    /// <summary>SailClient 顶级运行时日志配置（与 CapabilityOptions 平级）。</summary>
    public sealed class SailLogOptions
    {
        /// <summary>最低输出级别；默认 Message（开发用 useLogLevel(Debug) 看 BEGIN/END）。</summary>
        public SailLogLevel MinimumLevel { get; set; } = SailLogLevel.Message;

        /// <summary>是否在每条日志附带 UTC 时间戳（默认 true）。</summary>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>category 白名单；空或 null = 全部。</summary>
        public string[] CategoryIncludes { get; set; }
    }
}
