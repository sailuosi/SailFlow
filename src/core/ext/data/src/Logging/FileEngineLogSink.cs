using System;
using System.IO;
using System.Text;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.engine.Logging;
using saiflow.hub.Logging;

namespace saiflow.data.Logging
{
    /// <summary>
    /// 文件日志 Sink：按日滚动 engine- / job- 文件；目录读 <see cref="SailDataLifecycleOptions.LogFileDirectory"/>。
    /// </summary>
    public sealed class FileEngineLogSink : IEngineLogSink
    {
        private readonly object _gate = new();

        public FileEngineLogSink(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public FileEngineLogSink(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Log directory is required.", nameof(directory));
            DirectoryOverride = directory;
        }

        public SailClient Client { get; }
        public string DirectoryOverride { get; }

        public void Write(LogEntity entry)
        {
            if (entry == null)
                return;
            Append("engine", FormatLog(entry));
        }

        public void WriteJob(JobLogEntity entry)
        {
            if (entry == null)
                return;
            Append("job", FormatJob(entry));
        }

        private void Append(string prefix, string line)
        {
            try
            {
                var dir = ResolveDirectory();
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, prefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
                lock (_gate)
                {
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // 诊断日志失败不抛回业务
            }
        }

        private string ResolveDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DirectoryOverride))
                return DirectoryOverride;
            var opts = Client?.DataLifecycleOptions;
            return string.IsNullOrWhiteSpace(opts?.LogFileDirectory)
                ? "logs/sail"
                : opts.LogFileDirectory;
        }

        private static string FormatLog(LogEntity e)
        {
            var sb = new StringBuilder();
            sb.Append(e.Timestamp?.ToString("O") ?? DateTime.UtcNow.ToString("O"));
            sb.Append(" [").Append(e.Severity ?? "?").Append("] ");
            sb.Append(e.Title ?? e.Message ?? "");
            if (!string.IsNullOrEmpty(e.Message) && !string.Equals(e.Title, e.Message, StringComparison.Ordinal))
                sb.Append(" | ").Append(e.Message);
            if (!string.IsNullOrEmpty(e.StackTrace))
                sb.Append(" :: ").Append(e.StackTrace.Replace('\r', ' ').Replace('\n', ' '));
            return sb.ToString();
        }

        private static string FormatJob(JobLogEntity e)
        {
            var sb = new StringBuilder();
            sb.Append(e.CreatedDateTime?.ToString("O") ?? DateTime.UtcNow.ToString("O"));
            sb.Append(" [job:").Append(e.JobType ?? "?").Append('/').Append(e.JobName ?? "?").Append("] ");
            sb.Append("status=").Append(e.Status?.ToString() ?? "?");
            if (!string.IsNullOrEmpty(e.Message))
                sb.Append(" | ").Append(e.Message);
            if (!string.IsNullOrEmpty(e.StackTrace))
                sb.Append(" :: ").Append(e.StackTrace.Replace('\r', ' ').Replace('\n', ' '));
            return sb.ToString();
        }
    }
}
