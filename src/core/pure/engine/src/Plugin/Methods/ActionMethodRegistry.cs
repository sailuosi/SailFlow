using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Actions
{
    public sealed class ActionMethodRegistry
    {
        private readonly Dictionary<ActionMethodEnum, IActionMethodHandler> _handlers = new();

        public IReadOnlyCollection<ActionMethodEnum> Methods => _handlers.Keys.ToList();

        public ActionMethodRegistry Register(IActionMethodHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (handler.Method == ActionMethodEnum.None)
                throw new ArgumentException("IActionMethodHandler.Method cannot be None.", nameof(handler));
            _handlers[handler.Method] = handler;
            return this;
        }

        public bool TryGet(ActionMethodEnum method, out IActionMethodHandler handler)
            => _handlers.TryGetValue(method, out handler);

        public IActionMethodHandler GetRequired(ActionMethodEnum method)
        {
            if (TryGet(method, out var handler))
                return handler;

            var known = _handlers.Count == 0
                ? "(none registered)"
                : string.Join(", ", _handlers.Keys.OrderBy(x => (int)x));
            throw new InvalidOperationException(
                "No action method handler registered for \"" + method + "\". Registered: " + known +
                ". Call UseBizAction() / addActionMethod(...) for extensions (e.g. Python).");
        }
    }
}
