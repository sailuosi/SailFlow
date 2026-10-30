using System;

namespace saiflow.hub.Form
{
    /// <summary>
    /// Host-side registration point for <see cref="IFormService"/>.
    /// Call via sailflow.form <c>UseForm()</c> at SailClient Build time.
    /// </summary>
    public static class FormServiceLocator
    {
        private static IFormService _service;

        public static bool IsRegistered => _service != null;

        public static void Register(IFormService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static void Clear() => _service = null;

        public static IFormService Resolve()
        {
            if (_service == null)
            {
                throw new InvalidOperationException(
                    "IFormService is not registered. Reference sailflow.form and call builder.Services.UseForm() when building SailClient.");
            }

            return _service;
        }
    }
}
