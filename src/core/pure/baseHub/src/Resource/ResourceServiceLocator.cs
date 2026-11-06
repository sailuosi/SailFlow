using System;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Host-side registration point for <see cref="IResourceService"/>.
    /// Register via host UseXxx (e.g. UseSleveenOrgResource) or <c>UseResource(implementation)</c>.
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
                    "IResourceService is not registered. Host must UseXxx-inject an org/personnel implementation " +
                    "(e.g. UseSleveenOrgResource) or call UseResource(IResourceService). Engine has no org tables.");
            }

            return _service;
        }
    }
}
