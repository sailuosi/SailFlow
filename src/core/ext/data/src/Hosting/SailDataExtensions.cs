using System;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;

namespace saiflow.data.Hosting
{
    /// <summary>
    /// 注册 saiflow.data：默认 Table 日志 Sink。归档 API / 立刻归档 Hook 为独立扩展，按需调用。
    /// </summary>
    public static class SailDataExtensions
    {
        /// <summary>挂载数据管理能力（PostBuild：默认 TableEngineLogSink）。</summary>
        public static SailClientBuilder UseSailData(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddFeature(new SailFeatureDescriptor
            {
                Name = "sail.data",
                Phase = SailFeaturePhase.PostBuild,
                Order = 100,
                PostBuild = (client, _) => client.useDefaultTableLogSink()
            });

            return builder;
        }

        /// <summary>已构建 Client 上挂默认 Table Sink。</summary>
        public static SailClient UseSailData(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.useDefaultTableLogSink();
        }
    }
}
