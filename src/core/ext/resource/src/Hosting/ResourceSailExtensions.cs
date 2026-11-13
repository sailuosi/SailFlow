using System;
using sailflow.engine.Hosting;
using sailflow.hub.Resource;

namespace sailflow.hub.Hosting
{
    /// <summary>
    /// Resource 插槽骨架：引擎无组织人员表；须由宿主传入 <see cref="IResourceService"/> 实现。
    /// </summary>
    public static class ResourceSailExtensions
    {
        /// <summary>
        /// 注册宿主（或测试）提供的 <see cref="IResourceService"/>。
        /// </summary>
        public static ISailCollection UseResource(this ISailCollection services, IResourceService implementation)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(implementation);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Resource,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 110,
                ConfigureClient = (_, coll) =>
                {
                    coll.Properties[SailBuilderKeys.ResourceService] = implementation;
                    ResourceServiceLocator.Register(implementation);
                }
            });
        }
    }
}
