using System;
using System.Linq;

namespace sailflow.engine.Logging
{
    /// <summary>默认运行时日志：按 <see cref="SailLogOptions"/> 过滤后同步写控制台。</summary>
    public sealed class ConsoleSailLogger : ISailLogger
    {
        private readonly Func<SailLogOptions> _options;

        public ConsoleSailLogger(Func<SailLogOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public ConsoleSailLogger(SailLogOptions options)
            : this(() => options ?? new SailLogOptions())
        {
        }

        public bool IsEnabled(SailLogLevel level)
        {
            var o = _options() ?? new SailLogOptions();
            return level >= o.MinimumLevel;
        }

        public void Trace(string category, string message) =>
            Write(SailLogLevel.Trace, category, message, null);

        public void Debug(string category, string message) =>
            Write(SailLogLevel.Debug, category, message, null);

        public void Message(string category, string message) =>
            Write(SailLogLevel.Message, category, message, null);

        public void Warning(string category, string message) =>
            Write(SailLogLevel.Warning, category, message, null);

        public void Error(string category, string message) =>
            Write(SailLogLevel.Error, category, message, null);

        public void Error(string category, string message, Exception ex) =>
            Write(SailLogLevel.Error, category, message, ex);

        public void Debug(string category, string format, params object[] args) =>
            Debug(category, Format(format, args));

        public void Message(string category, string format, params object[] args) =>
            Message(category, Format(format, args));

        public void Warning(string category, string format, params object[] args) =>
            Warning(category, Format(format, args));

        public void Error(string category, Exception ex, string format, params object[] args) =>
            Error(category, Format(format, args), ex);

        private void Write(SailLogLevel level, string category, string message, Exception ex)
        {
            var o = _options() ?? new SailLogOptions();
            if (level < o.MinimumLevel)
                return;
            if (!CategoryAllowed(o, category))
                return;

            var ts = o.IncludeTimestamp
                ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + " "
                : string.Empty;
            var line = $"{ts}[{level}] [{category ?? "-"}] {message}";
            if (ex != null)
                line += " | " + ex.GetType().Name + ": " + ex.Message;

            // 同步写出，便于卡死排障时 BEGIN 已落盘
            if (level >= SailLogLevel.Error)
                Console.Error.WriteLine(line);
            else
                Console.WriteLine(line);
        }

        private static bool CategoryAllowed(SailLogOptions o, string category)
        {
            var includes = o.CategoryIncludes;
            if (includes == null || includes.Length == 0)
                return true;
            if (string.IsNullOrEmpty(category))
                return false;
            return includes.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
        }

        private static string Format(string format, object[] args)
        {
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format ?? string.Empty;
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }
    }
}
