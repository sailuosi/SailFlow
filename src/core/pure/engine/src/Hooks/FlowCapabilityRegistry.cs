using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using saiflow.engine.Client;

namespace saiflow.engine.Hooks
{
    /// <summary>
    /// Client 级 Gate / Hook / Notifier 注册表；同名后注册覆盖并 Warning。
    /// </summary>
    public sealed class FlowCapabilityRegistry
    {
        private readonly Dictionary<string, GateRegistration> _gates = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HookRegistration> _hooks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NotifierRegistration> _notifiers = new(StringComparer.OrdinalIgnoreCase);

        public SailClient Client { get; }

        public FlowCapabilityRegistry(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IReadOnlyCollection<GateRegistration> Gates => _gates.Values.OrderBy(g => g.Order).ToList();
        public IReadOnlyCollection<HookRegistration> Hooks => _hooks.Values.OrderBy(h => h.Order).ToList();
        public IReadOnlyCollection<NotifierRegistration> Notifiers => _notifiers.Values.OrderBy(n => n.Order).ToList();

        public void RegisterGate(IFlowGate gate, HookRegistrationOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(gate);
            RegisterGate(BuildGateRegistration(gate, options ?? new HookRegistrationOptions()));
        }

        public void RegisterGate(GateRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("Gate Name is required.", nameof(registration));

            if (_gates.ContainsKey(registration.Name))
                TraceWarning($"Gate '{registration.Name}' overwritten by a later registration.");

            _gates[registration.Name] = registration;
        }

        public void RegisterHook(IFlowHook hook, HookRegistrationOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(hook);
            RegisterHook(BuildHookRegistration(hook, options ?? new HookRegistrationOptions()));
        }

        public void RegisterHook(HookRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("Hook Name is required.", nameof(registration));

            if (_hooks.ContainsKey(registration.Name))
                TraceWarning($"Hook '{registration.Name}' overwritten by a later registration.");

            _hooks[registration.Name] = registration;
        }

        public void RegisterNotifier(ITaskNotifier notifier, HookRegistrationOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(notifier);
            RegisterNotifier(BuildNotifierRegistration(notifier, options ?? new HookRegistrationOptions()));
        }

        public void RegisterNotifier(NotifierRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("Notifier Name is required.", nameof(registration));

            if (_notifiers.ContainsKey(registration.Name))
                TraceWarning($"Notifier '{registration.Name}' overwritten by a later registration.");

            _notifiers[registration.Name] = registration;
        }

        internal static GateRegistration BuildGateRegistration(IFlowGate gate, HookRegistrationOptions o) =>
            new()
            {
                Name = gate.Name,
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                ActivityCodes = o.ActivityCodes,
                Handler = gate,
            };

        internal static HookRegistration BuildHookRegistration(IFlowHook hook, HookRegistrationOptions o) =>
            new()
            {
                Name = hook.Name,
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                ActivityCodes = o.ActivityCodes,
                Events = o.Events,
                Mode = o.Mode,
                Handler = hook,
            };

        internal static NotifierRegistration BuildNotifierRegistration(ITaskNotifier notifier, HookRegistrationOptions o) =>
            new()
            {
                Name = notifier.Name,
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                ActivityCodes = o.ActivityCodes,
                TaskKinds = o.TaskKinds,
                Mode = o.Mode == HookMode.Sync ? HookMode.AsyncOutbox : o.Mode,
                Handler = notifier,
            };

        private static void TraceWarning(string message) =>
            Trace.TraceWarning("[saiflow.engine.Hooks] " + message);
    }
}
