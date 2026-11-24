using System;

namespace sailflow.log
{
    /// <summary>无操作运行时日志（测试或显式关闭）。</summary>
    public sealed class NullSailLogger : ISailLogger
    {
        public static readonly NullSailLogger Instance = new NullSailLogger();

        public bool IsEnabled(SailLogLevel level) => false;

        public void Trace(string category, string message) { }
        public void Debug(string category, string message) { }
        public void Message(string category, string message) { }
        public void Warning(string category, string message) { }
        public void Error(string category, string message) { }
        public void Error(string category, string message, Exception ex) { }

        public void Debug(string category, string format, params object[] args) { }
        public void Message(string category, string format, params object[] args) { }
        public void Warning(string category, string format, params object[] args) { }
        public void Error(string category, Exception ex, string format, params object[] args) { }
    }
}
