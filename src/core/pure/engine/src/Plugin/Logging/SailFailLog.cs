using System;
using sailflow.engine.Client;
using sailflow.engine.Result;

namespace sailflow.engine.Logging
{
    /// <summary>
    /// 引擎中断 / 失败 / 抛错的统一运行时日志入口（与 <see cref="IEngineLogSink"/> 落库并行）。
    /// </summary>
    public static class SailFailLog
    {
        /// <summary>业务校验未过、门禁拒绝、硬锁拦截等：不抛，中止继续。</summary>
        public static void Interrupt(
            SailClient sail,
            string category,
            string reason,
            string message,
            params object[] args)
        {
            var log = sail?.Log;
            if (log == null)
                return;
            var text = args is { Length: > 0 }
                ? SafeFormat(message, args)
                : message;
            log.Warning(category, "INTERRUPT reason={0} {1}", reason ?? "-", text ?? "-");
        }

        /// <summary>动作结果非 Success（含 Exception/Failed）。</summary>
        public static void ResultNotSuccess(
            SailClient sail,
            string category,
            string action,
            WfExecutedResult result)
        {
            if (result == null || result.Status == WfExecutedStatus.Success)
                return;
            var log = sail?.Log;
            if (log == null)
                return;

            var level = result.Status == WfExecutedStatus.Failed || result.Status == WfExecutedStatus.Exception
                ? SailLogLevel.Error
                : SailLogLevel.Warning;
            var line =
                $"RESULT action={action ?? "-"} status={result.Status} reason={result.ReasonCode ?? "-"} " +
                $"code={result.ValidationCode ?? "-"} msg={result.Message ?? "-"}";
            if (level >= SailLogLevel.Error)
                log.Error(category, line);
            else
                log.Warning(category, line);
        }

        /// <summary>捕获后转为 Failed / 即将上抛。</summary>
        public static void Exception(
            SailClient sail,
            string category,
            string action,
            Exception ex,
            string detail = null)
        {
            var log = sail?.Log;
            if (log == null || ex == null)
                return;
            log.Error(
                category,
                ex,
                "EXCEPTION action={0} {1}",
                action ?? "-",
                detail ?? ex.Message);
        }

        /// <summary>记日志后原样抛出（Manager 等硬失败路径）。</summary>
        public static Exception Throw(
            SailClient sail,
            string category,
            Exception ex,
            string format,
            params object[] args)
        {
            var log = sail?.Log;
            if (log != null && ex != null)
            {
                var detail = args is { Length: > 0 } ? SafeFormat(format, args) : format;
                log.Error(category, ex, "THROW {0}", detail ?? ex.Message);
            }
            return ex;
        }

        static string SafeFormat(string format, object[] args)
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
