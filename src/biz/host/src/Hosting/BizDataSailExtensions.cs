using System;
using sailflow.data.Hosting;
using sailflow.engine.Client;
using sailflow.engine.Hosting;

namespace sailflow.biz.Hosting
{
    /// <summary>挂载 sailflow.data（冷热归档 / Table 日志 Sink / 数据管理 Hook）。</summary>
    public static class BizDataSailExtensions
    {
        public static SailClientBuilder UseBizData(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseSailData();
        }

        public static SailClient UseBizData(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.UseSailData();
        }
    }
}
