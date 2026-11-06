using System;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Host registration point for <see cref="IDeptService"/>.
    /// 宿主在 Build 时经 UseXxx 注入具体实现（如 sleveen SysOrg/SysUser）。
    /// </summary>
    public static class DeptServiceLocator
    {
        private static IDeptService _service;

        public static bool IsRegistered => _service != null;

        public static void Register(IDeptService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static void Clear() => _service = null;

        public static IDeptService TryResolve() => _service;

        public static IDeptService Resolve()
        {
            if (_service == null)
            {
                throw new InvalidOperationException(
                    "IDeptService is not registered. Host must UseXxx-register an org/personnel implementation " +
                    "(engine has no org tables; call DeptServiceLocator.Register from host capability pack).");
            }

            return _service;
        }
    }
}
