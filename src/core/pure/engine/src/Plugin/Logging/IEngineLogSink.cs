using System;
using sailflow.engine.Entity;
using sailflow.hub.Logging;

namespace sailflow.engine.Logging
{
    /// <summary>
    /// 引擎诊断日志契约。写表仅为默认实现；可替换为文件 / Host Logger 等。
    /// </summary>
    public interface IEngineLogSink
    {
        /// <summary>写入引擎异常/事件日志（通常异步调用侧已离主路径）。</summary>
        void Write(LogEntity entry);

        /// <summary>写入作业诊断日志。</summary>
        void WriteJob(JobLogEntity entry);
    }

    /// <summary>无操作 Sink（测试或显式关闭落库时）。</summary>
    public sealed class NullEngineLogSink : IEngineLogSink
    {
        public static readonly NullEngineLogSink Instance = new NullEngineLogSink();

        public void Write(LogEntity entry) { }

        public void WriteJob(JobLogEntity entry) { }
    }
}
