using System;
using saiflow.data.Lifecycle;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using saiflow.engine.Event;
using saiflow.engine.Hooks;

namespace saiflow.data.Hosting
{
    /// <summary>
    /// 办结立刻归档 Hook（<b>opt-in</b>）。产品默认靠定时 <see cref="SailDataClientExtensions.RunProcessArchiveBatch"/>。
    /// </summary>
    public static class SailImmediateArchiveExtensions
    {
        /// <summary>
        /// 注册立刻归档 Hook。默认 AsyncOutbox + OnProcessCompleted/Closed。
        /// </summary>
        public static SailClientBuilder UseImmediateProcessArchiveHook(
            this SailClientBuilder builder,
            Action<HookRegistrationOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddFeature(new SailFeatureDescriptor
            {
                Name = "sail.lifecycle.archive.immediate",
                Phase = SailFeaturePhase.PostBuild,
                Order = 220,
                PostBuild = (client, _) => Register(client, configure)
            });

            return builder;
        }

        public static SailClient useImmediateProcessArchiveHook(
            this SailClient client,
            Action<HookRegistrationOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            Register(client, configure);
            return client;
        }

        static void Register(SailClient client, Action<HookRegistrationOptions> configure)
        {
            client.useWorkflowHook(new ImmediateProcessArchiveHook(client), o =>
            {
                o.Match = HookMatch.All;
                o.Mode = HookMode.AsyncOutbox;
                o.Events =
                [
                    EventFireTypeEnum.OnProcessCompleted,
                    EventFireTypeEnum.OnProcessClosed
                ];
                configure?.Invoke(o);
            });
        }
    }
}
