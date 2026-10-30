using System;
using saiflow.engine.Client.Hosting;

namespace saiflow.hub.Resource.Hosting
{
    /// <summary>
    /// Registers resource implementation into the SailClient pipeline.
    /// </summary>
    public static class ResourceSailExtensions
    {
        public static ISailCollection UseResource(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = "Resource",
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 110,
                ConfigureClient = (_, coll) =>
                {
                    var resourceService = ResourceServiceFactory.Create();
                    coll.Properties[SailBuilderKeys.ResourceService] = resourceService;
                    ResourceServiceLocator.Register(resourceService);
                }
            });
        }
    }
}
