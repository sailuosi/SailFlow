using System;
using sailflow.engine.Logging;

namespace sailflow.msg.Logging
{
    /// <summary>
    /// 控制台 <see cref="ISailLogger"/>（sailflow.msg）：继承 <see cref="SailLoggerBase"/>，可选按级别着色。
    /// 未 seal，宿主可继承并改写 <see cref="Emit"/>（如 Sleveen DevConsole 带标题块）。
    /// 启用：<c>builder.UseConsoleSailLog()</c> / <c>client.useConsoleSailLog()</c>。
    /// </summary>
    public class ConsoleSailLog : SailLoggerBase
    {
        public ConsoleSailLog(Func<SailLogOptions> options, ConsoleSailLogOptions self = null)
            : base(options)
        {
            ConsoleOptions = self ?? new ConsoleSailLogOptions();
        }

        public ConsoleSailLog(SailLogOptions options, ConsoleSailLogOptions self = null)
            : base(options)
        {
            ConsoleOptions = self ?? new ConsoleSailLogOptions();
        }

        /// <summary>控制台自身选项（着色等）。</summary>
        protected ConsoleSailLogOptions ConsoleOptions { get; }

        protected override void Emit(
            SailLogLevel level,
            string category,
            string message,
            string formattedLine,
            Exception ex)
        {
            WriteConsoleLine(level, formattedLine);
        }

        /// <summary>按级别着色写一行到 stdout/stderr。</summary>
        protected void WriteConsoleLine(SailLogLevel level, string line)
        {
            var toError = level >= SailLogLevel.Error;
            if (!ConsoleOptions.UseColors)
            {
                if (toError)
                    Console.Error.WriteLine(line);
                else
                    Console.WriteLine(line);
                return;
            }

            var prev = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ColorFor(level);
                if (toError)
                    Console.Error.WriteLine(line);
                else
                    Console.WriteLine(line);
            }
            finally
            {
                Console.ForegroundColor = prev;
            }
        }

        /// <summary>级别 → 控制台前景色；子类可覆盖。</summary>
        protected virtual ConsoleColor ColorFor(SailLogLevel level) =>
            level switch
            {
                SailLogLevel.Trace => ConsoleColor.DarkGray,
                SailLogLevel.Debug => ConsoleColor.Gray,
                SailLogLevel.Message => ConsoleColor.White,
                SailLogLevel.Warning => ConsoleColor.Yellow,
                SailLogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White,
            };
    }

    /// <summary><see cref="ConsoleSailLog"/> 自身选项（与 Client.<see cref="SailLogOptions"/> 分离）。</summary>
    public class ConsoleSailLogOptions
    {
        /// <summary>按级别着色输出；默认 true。无控制台颜色能力时可关。</summary>
        public bool UseColors { get; set; } = true;
    }
}
