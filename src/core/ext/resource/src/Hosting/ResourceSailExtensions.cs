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

        /// <summary>
        /// 已废弃：引擎不再提供默认 sys_* 实现。请 <c>UseResource(hostImpl)</c> 或宿主 <c>UseSleveenOrgResource()</c>。
        /// </summary>
        [Obsolete("Engine has no org/personnel tables. Use UseResource(IResourceService) or host UseSleveenOrgResource().")]
        public static ISailCollection UseResource(this ISailCollection services)
        {
            throw new InvalidOperationException(
                "IResourceService has no built-in engine implementation (no sys_user/sys_role tables). " +
                "Register a host implementation: services.UseResource(impl) or builder.UseSleveenOrgResource().");
        }
    }
}
