using System;

namespace sailflow.hub.Localize
{
    /// <summary>
    /// Host-side registration point for <see cref="ILocalizeMessages"/>.
    /// Call via sailflow.localize <c>UseLocalize()</c> at SailClient Build time.
    /// When unregistered, <see cref="Current"/> returns <see cref="PassthroughLocalizeMessages"/>.
    /// </summary>
    public static class LocalizeMessagesLocator
    {
        private static ILocalizeMessages _service;

        public static bool IsRegistered => _service != null;

        public static void Register(ILocalizeMessages service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static void Clear() => _service = null;

        /// <summary>
        /// Registered implementation, or passthrough (key-as-message) when not configured.
        /// </summary>
        public static ILocalizeMessages Current
            => _service ?? PassthroughLocalizeMessages.Instance;

        /// <summary>
        /// Requires a real registration (JSON-backed). Prefer <see cref="Current"/> for engine paths.
        /// </summary>
        public static ILocalizeMessages Resolve()
        {
            if (_service == null)
            {
                throw new InvalidOperationException(
                    "ILocalizeMessages is not registered. Call UseLocalize() (default JSON) or UseLocalize(ILocalizeMessages) with a host implementation.");
            }

            return _service;
        }
    }
}
