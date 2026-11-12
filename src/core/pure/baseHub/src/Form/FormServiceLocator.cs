using System;

namespace sailflow.hub.Form
{
    /// <summary>
    /// Host-side registration point for <see cref="IFormService"/>.
    /// Default: sailflow.form <c>UseForm()</c>（FbForm 流程附属库）；或 <c>UseForm(hostImpl)</c> 覆盖。
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
                    "IFormService is not registered. Call builder.Services.UseForm() (default FbForm store) " +
                    "or UseForm(IFormService) with a host implementation.");
            }

            return _service;
        }
    }
}
