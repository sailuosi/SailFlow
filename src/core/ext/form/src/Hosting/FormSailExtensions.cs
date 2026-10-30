using System;
using saiflow.engine.Client.Hosting;

namespace saiflow.hub.Form.Hosting
{
    /// <summary>
    /// Registers form implementation into the SailClient pipeline.
    /// </summary>
    public static class FormSailExtensions
    {
        public static ISailCollection UseForm(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = "Form",
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 120,
                ConfigureClient = (client, coll) =>
                {
                    var formService = FormServiceFactory.Create(client);
                    coll.Properties[SailBuilderKeys.FormService] = formService;
                    FormServiceLocator.Register(formService);
                }
            });
        }
    }
}
