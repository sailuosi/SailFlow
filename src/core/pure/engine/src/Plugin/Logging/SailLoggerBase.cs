using System;
using System.Linq;

namespace sailflow.log
{
    /// <summary>
    /// <see cref="ISailLogger"/> 抽象基类：级别过滤、category 白名单、占位符格式化、行模板。
    /// 业务自定义 Sink 时继承本类，只实现 <see cref="Emit"/>（或覆盖 <see cref="FormatLine"/>）。
    /// </summary>
    public abstract class SailLoggerBase : ISailLogger
    {
        private readonly Func<SailLogOptions> _options;

        protected SailLoggerBase(Func<SailLogOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected SailLoggerBase(SailLogOptions options)
            : this(() => options ?? new SailLogOptions())
        {
        }

        /// <summary>当前生效的日志选项（每次读取，便于运行期改 MinimumLevel）。</summary>
        protected SailLogOptions Options => _options() ?? new SailLogOptions();

        public virtual bool IsEnabled(SailLogLevel level) =>
            level >= Options.MinimumLevel;

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
            Debug(category, FormatArgs(format, args));

        public void Message(string category, string format, params object[] args) =>
            Message(category, FormatArgs(format, args));

        public void Warning(string category, string format, params object[] args) =>
            Warning(category, FormatArgs(format, args));

        public void Error(string category, Exception ex, string format, params object[] args) =>
            Error(category, FormatArgs(format, args), ex);

        /// <summary>过滤后格式化并交给 <see cref="Emit"/>。</summary>
        protected void Write(SailLogLevel level, string category, string message, Exception ex)
        {
            if (!IsEnabled(level))
                return;
            if (!IsCategoryAllowed(category))
                return;

            var line = FormatLine(level, category, message, ex);
            Emit(level, category, message, line, ex);
        }

        /// <summary>
        /// 真正输出。默认行已由 <see cref="FormatLine"/> 生成；
        /// 也可忽略 <paramref name="formattedLine"/>，自行用 level/category/message/ex 组装。
        /// </summary>
        protected abstract void Emit(
            SailLogLevel level,
            string category,
            string message,
            string formattedLine,
            Exception ex);

        /// <summary>默认行：<c>{utc?}[Level] [category] message | ExType: ExMsg</c>。</summary>
        protected virtual string FormatLine(
            SailLogLevel level,
            string category,
            string message,
            Exception ex)
        {
            var o = Options;
            var ts = o.IncludeTimestamp
                ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + " "
                : string.Empty;
            var line = $"{ts}[{level}] [{category ?? "-"}] {message}";
            if (ex != null)
                line += " | " + ex.GetType().Name + ": " + ex.Message;
            return line;
        }

        /// <summary><see cref="SailLogOptions.CategoryIncludes"/> 白名单；空=全部。</summary>
        protected virtual bool IsCategoryAllowed(string category)
        {
            var includes = Options.CategoryIncludes;
            if (includes == null || includes.Length == 0)
                return true;
            if (string.IsNullOrEmpty(category))
                return false;
            return includes.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary><c>string.Format</c>；格式异常时退回原 format。</summary>
        protected virtual string FormatArgs(string format, object[] args)
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
