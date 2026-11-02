using System;
using saiflow.data.HotShard;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;

namespace saiflow.data.Hosting
{
    /// <summary>
    /// 注册 saiflow.data：日志 Sink + 可选热表白名单分表。归档 API / 立刻归档 Hook 为独立扩展。
    /// </summary>
    public static class SailDataExtensions
    {
        /// <summary>挂载数据管理能力（PostBuild：日志 Sink + HotShard）。</summary>
        public static SailClientBuilder UseSailData(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.SailData,
                Phase = SailFeaturePhase.PostBuild,
                Order = 100,
                DependsOn = new[] { SailFeatureNames.Database },
                PostBuild = (client, _) => Apply(client)
            });

            return builder;
        }

        /// <summary>已构建 Client 上挂默认日志 Sink 与白名单热分表。</summary>
        public static SailClient UseSailData(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            return Apply(client);
        }

        static SailClient Apply(SailClient client)
        {
            client.useDefaultLogSink();
            ApplyHotShard(client);
            return client;
        }

        static void ApplyHotShard(SailClient client)
        {
            var ids = client.DataLifecycleOptions?.HotShardProcessIds;
            if (ids == null || ids.Count == 0)
                return;

            client.useHotShard(new LifecycleHotShardRouter(client));
            HotShardTableEnsurer.EnsureAll(client);
        }
    }
}
