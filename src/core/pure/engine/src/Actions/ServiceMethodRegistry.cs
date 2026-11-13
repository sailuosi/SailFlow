using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Actions
{
    public sealed class ServiceMethodRegistry
    {
        private readonly Dictionary<ServiceMethodEnum, IServiceMethodHandler> _handlers = new();

        public IReadOnlyCollection<ServiceMethodEnum> Methods => _handlers.Keys.ToList();

        public ServiceMethodRegistry Register(IServiceMethodHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (handler.Method == ServiceMethodEnum.None)
                throw new ArgumentException("IServiceMethodHandler.Method cannot be None.", nameof(handler));
            _handlers[handler.Method] = handler;
            return this;
        }

        public bool TryGet(ServiceMethodEnum method, out IServiceMethodHandler handler)
            => _handlers.TryGetValue(method, out handler);

        public IServiceMethodHandler GetRequired(ServiceMethodEnum method)
        {
            if (TryGet(method, out var handler))
                return handler;

            var known = _handlers.Count == 0
                ? "(none registered)"
                : string.Join(", ", _handlers.Keys.OrderBy(x => (int)x));
            throw new InvalidOperationException(
                "No service method handler registered for \"" + method + "\". Registered: " + known +
                ". Call addServiceMethod(...) to register.");
        }
    }
}
