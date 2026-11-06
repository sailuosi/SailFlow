using System;
using saiflow.engine.Hosting;
using saiflow.hub.Resource;

namespace saiflow.hub.Hosting
{
    /// <summary>
    /// Registers the built-in demo <see cref="ResourceService"/> (engine resource tables).
    /// 组织人员主数据场景请由宿主 <c>UseXxx</c> 覆盖注册 <see cref="IResourceService"/> /
    /// <see cref="IDeptService"/>，勿依赖本默认实现对职级选人。
    /// </summary>
    public static class ResourceSailExtensions
    {
        public static ISailCollection UseResource(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Resource,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 110,
                ConfigureClient = (client, coll) =>
                {
                    var resourceService = ResourceServiceFactory.Create(client);
                    coll.Properties[SailBuilderKeys.ResourceService] = resourceService;
                    ResourceServiceLocator.Register(resourceService);
                }
            });
        }
    }
}
