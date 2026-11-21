using System;

namespace sailflow.engine.Logging
{
    /// <summary>
    /// 引擎默认控制台日志：继承 <see cref="SailLoggerBase"/>，同步 <see cref="Console"/> 写出。
    /// 业务扩展请优先继承基类或使用 sailflow.msg 的 <c>ConsoleSailLog</c>（着色）。
    /// </summary>
    public sealed class ConsoleSailLogger : SailLoggerBase
    {
        public ConsoleSailLogger(Func<SailLogOptions> options)
            : base(options)
        {
        }

        public ConsoleSailLogger(SailLogOptions options)
            : base(options)
        {
        }

        protected override void Emit(
            SailLogLevel level,
            string category,
            string message,
            string formattedLine,
            Exception ex)
        {
            // 同步写出，便于卡死排障时 BEGIN 已落盘
            if (level >= SailLogLevel.Error)
                Console.Error.WriteLine(formattedLine);
            else
                Console.WriteLine(formattedLine);
        }
    }
}
