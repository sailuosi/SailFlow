using System;
using sailflow.engine.Hosting;
using sailflow.hub.Localize;

namespace sailflow.hub.Hosting
{
    /// <summary>
    /// Localize 插槽：默认 JSON 文案实现；宿主可用 <see cref="UseLocalize(ISailCollection, ILocalizeMessages)"/> 覆盖。
    /// 语言用 <see cref="UseLang"/> 配置（写入 pending，Build / Apply 时落到 Messages）。
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

        /// <summary>
        /// 配置引擎文案语言（pending，Build 时生效；可与 UseLocalize 任意顺序调用）。
        /// Builder 侧请用 <see cref="SailClientBuilder.UseLang"/>。
        /// </summary>
        public static ISailCollection UseLang(this ISailCollection services, LangTypeEnum lang)
        {
            ArgumentNullException.ThrowIfNull(services);
            if (lang == LangTypeEnum.none)
                throw new ArgumentOutOfRangeException(nameof(lang), "UseLang requires zh or en.");

            services.Properties[SailBuilderKeys.Lang] = lang;
            return services;
        }

        static void Apply(sailflow.engine.Client.SailClient client, ISailCollection coll, ILocalizeMessages messages)
        {
            coll.Properties[SailBuilderKeys.LocalizeMessages] = messages;
            client.Messages = messages;
            LocalizeMessagesLocator.Register(messages);
            messages.SetDefault();
            ApplyPendingLang(messages, coll);
        }

        /// <summary>将 Builder 期 UseLang 落到 Messages（覆盖 SetDefault）。</summary>
        internal static void ApplyPendingLang(ILocalizeMessages messages, ISailCollection coll)
        {
            if (coll.Properties.TryGetValue(SailBuilderKeys.Lang, out var box)
                && box is LangTypeEnum lang
                && lang != LangTypeEnum.none)
            {
                messages.SetLang(lang);
            }
        }
    }
}
