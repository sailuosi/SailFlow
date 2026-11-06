using System;
using saiflow.engine.Client;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Compatibility shim: prefer <see cref="DeptServiceLocator"/> (baseHub contract).
    /// </summary>
    [Obsolete("Use DeptServiceLocator (baseHub). Engine holds IDeptService contract only.")]
    public static class DeptServiceFactory
    {
        public static void Register(IDeptService service) => DeptServiceLocator.Register(service);

        public static void Clear() => DeptServiceLocator.Clear();

        public static bool IsRegistered => DeptServiceLocator.IsRegistered;

        public static IDeptService TryResolve() => DeptServiceLocator.TryResolve();

        public static IDeptService CreateDeptService(SailClient client)
            => DeptServiceLocator.Resolve();
    }
}
