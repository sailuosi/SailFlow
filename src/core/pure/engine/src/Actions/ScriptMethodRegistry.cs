using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Xpdl.Common;

namespace saiflow.engine.Actions
{
    public sealed class ScriptMethodRegistry
    {
        private readonly Dictionary<ScriptMethodEnum, IScriptMethodHandler> _handlers = new();

        public IReadOnlyCollection<ScriptMethodEnum> Methods => _handlers.Keys.ToList();

        public ScriptMethodRegistry Register(IScriptMethodHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (handler.Method == ScriptMethodEnum.None)
                throw new ArgumentException("IScriptMethodHandler.Method cannot be None.", nameof(handler));
            _handlers[handler.Method] = handler;
            return this;
        }

        public bool TryGet(ScriptMethodEnum method, out IScriptMethodHandler handler)
            => _handlers.TryGetValue(method, out handler);

        public IScriptMethodHandler GetRequired(ScriptMethodEnum method)
        {
            if (TryGet(method, out var handler))
                return handler;

            var known = _handlers.Count == 0
                ? "(none registered)"
                : string.Join(", ", _handlers.Keys.OrderBy(x => (int)x));
            throw new InvalidOperationException(
                "No script method handler registered for \"" + method + "\". Registered: " + known +
                ". Call UseBizAction() / useScriptMethod(...) for extensions (e.g. Python).");
        }
    }
}
