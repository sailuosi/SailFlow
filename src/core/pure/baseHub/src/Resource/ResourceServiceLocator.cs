using System;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Host-side registration point for <see cref="IResourceService"/>.
    /// Call via sailflow.resource <c>UseResource()</c> at SailClient Build time.
    /// </summary>
    public static class ResourceServiceLocator
    {
        private static IResourceService _service;

        public static bool IsRegistered => _service != null;

        public static void Register(IResourceService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static void Clear() => _service = null;

        public static IResourceService Resolve()
        {
            if (_service == null)
            {
                throw new InvalidOperationException(
                    "IResourceService is not registered. Reference sailflow.resource and call builder.Services.UseResource() when building SailClient.");
            }

            return _service;
        }
    }
}
