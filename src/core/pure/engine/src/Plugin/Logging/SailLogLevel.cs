namespace sailflow.engine.Logging
{
    /// <summary>
    /// 运行时诊断日志级别（与 <see cref="IEngineLogSink"/> 落库实体分工，见钩子治理专题 D10）。
    /// </summary>
    public enum SailLogLevel
    {
        Trace = 0,
        Debug = 1,
        /// <summary>消息（正常进度）。</summary>
        Message = 2,
        /// <summary>警告。</summary>
        Warning = 3,
        /// <summary>错误。</summary>
        Error = 4,
    }
}
