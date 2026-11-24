using System;

namespace sailflow.log
{
    /// <summary>
    /// 运行时诊断日志契约（Gate / Hook / Notifier 等）。非 <see cref="IEngineLogSink"/> 落库通道。
    /// </summary>
    public interface ISailLogger
    {
        bool IsEnabled(SailLogLevel level);

        void Trace(string category, string message);
        void Debug(string category, string message);
        void Message(string category, string message);
        void Warning(string category, string message);
        void Error(string category, string message);
        void Error(string category, string message, Exception ex);

        void Debug(string category, string format, params object[] args);
        void Message(string category, string format, params object[] args);
        void Warning(string category, string format, params object[] args);
        void Error(string category, Exception ex, string format, params object[] args);
    }
}
