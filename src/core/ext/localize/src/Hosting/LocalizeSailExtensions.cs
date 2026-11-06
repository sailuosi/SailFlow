using System;
using saiflow.engine.Hosting;
using saiflow.hub.Localize;

namespace saiflow.hub.Hosting
{
    /// <summary>
    /// Localize 插槽：默认 JSON 文案实现；宿主可用 <see cref="UseLocalize(ISailCollection, ILocalizeMessages)"/> 覆盖。
    /// 无业务主数据表。
    /// </summary>
    public static class LocalizeSailExtensions
    {
        /// <summary>注册默认 JSON 本地化实现。</summary>
        public static ISailCollection UseLocalize(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Localize,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 50,
                ConfigureClient = (client, coll) =>
                {
                    var messages = new JsonLocalizeMessages();
                    Apply(client, coll, messages);
                }
            });
        }

        /// <summary>注册宿主自定义 <see cref="ILocalizeMessages"/>。</summary>
        public static ISailCollection UseLocalize(this ISailCollection services, ILocalizeMessages implementation)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(implementation);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Localize,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 50,
                ConfigureClient = (client, coll) => Apply(client, coll, implementation)
            });
        }

        static void Apply(saiflow.engine.Client.SailClient client, ISailCollection coll, ILocalizeMessages messages)
        {
            coll.Properties[SailBuilderKeys.LocalizeMessages] = messages;
            client.Messages = messages;
            LocalizeMessagesLocator.Register(messages);
            messages.SetDefault();
        }
    }
}
