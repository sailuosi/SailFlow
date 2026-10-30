using System;
using saiflow.engine.Client.Hosting;

namespace saiflow.hub.Localize.Hosting
{
    /// <summary>
    /// Registers JSON-backed localization into the SailClient pipeline.
    /// </summary>
    public static class LocalizeSailExtensions
    {
        public static ISailCollection UseLocalize(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = "Localize",
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 50,
                ConfigureClient = (client, coll) =>
                {
                    var messages = new JsonLocalizeMessages();
                    coll.Properties[SailBuilderKeys.LocalizeMessages] = messages;
                    client.Messages = messages;
                    // Keep Locator in sync for rare static helpers without SailClient.
                    LocalizeMessagesLocator.Register(messages);
                    messages.SetDefault();
                }
            });
        }
    }
}
