using System;
using System.Collections.Generic;
using System.Linq;
using sailflow.engine.Client;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// Client 级 Gate / Hook / Notifier 注册表；同名后注册覆盖并 Warning。
    /// </summary>
    public sealed class FlowCapabilityRegistry
    {
        private readonly Dictionary<string, GateRegistration> _gates = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HookRegistration> _hooks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NotifierRegistration> _notifiers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CopytoNotifierRegistration> _copytoNotifiers =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProcessInstanceCreateHookRegistration> _createHooks =
            new(StringComparer.OrdinalIgnoreCase);

        public SailClient Client { get; }

        public FlowCapabilityRegistry(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public IReadOnlyCollection<GateRegistration> Gates => _gates.Values.OrderBy(g => g.Order).ToList();
        public IReadOnlyCollection<HookRegistration> Hooks => _hooks.Values.OrderBy(h => h.Order).ToList();
        public IReadOnlyCollection<NotifierRegistration> Notifiers => _notifiers.Values.OrderBy(n => n.Order).ToList();
        public IReadOnlyCollection<CopytoNotifierRegistration> CopytoNotifiers =>
            _copytoNotifiers.Values.OrderBy(n => n.Order).ToList();
        public IReadOnlyCollection<ProcessInstanceCreateHookRegistration> ProcessInstanceCreateHooks =>
            _createHooks.Values.OrderBy(h => h.Order).ToList();

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

        public void RegisterCopytoNotifier(ICopytoNotifier notifier, HookRegistrationOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(notifier);
            RegisterCopytoNotifier(BuildCopytoNotifierRegistration(notifier, options ?? new HookRegistrationOptions()));
        }

        public void RegisterCopytoNotifier(CopytoNotifierRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("CopytoNotifier Name is required.", nameof(registration));

            if (_copytoNotifiers.ContainsKey(registration.Name))
                TraceWarning($"CopytoNotifier '{registration.Name}' overwritten by a later registration.");

            _copytoNotifiers[registration.Name] = registration;
        }

        public void RegisterProcessInstanceCreateHook(
            IProcessInstanceCreateHook hook,
            HookRegistrationOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(hook);
            RegisterProcessInstanceCreateHook(
                BuildCreateHookRegistration(hook, options ?? new HookRegistrationOptions()));
        }

        public void RegisterProcessInstanceCreateHook(ProcessInstanceCreateHookRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            if (string.IsNullOrWhiteSpace(registration.Name))
                throw new ArgumentException("ProcessInstanceCreateHook Name is required.", nameof(registration));

            if (_createHooks.ContainsKey(registration.Name))
                TraceWarning($"ProcessInstanceCreateHook '{registration.Name}' overwritten by a later registration.");

            _createHooks[registration.Name] = registration;
        }

        internal static GateRegistration BuildGateRegistration(IFlowGate gate, HookRegistrationOptions o) =>
            new()
            {
                Name = gate.Name,
                Title = CapabilityPluginNames.ResolveTitle(o.Title, gate.Title),
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
                Title = CapabilityPluginNames.ResolveTitle(o.Title, hook.Title),
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                ActivityCodes = o.ActivityCodes,
                Events = o.Events,
                Mode = o.Mode,
                Handler = hook,
                TimeoutMs = o.TimeoutMs,
            };

        internal static NotifierRegistration BuildNotifierRegistration(ITaskNotifier notifier, HookRegistrationOptions o) =>
            new()
            {
                Name = notifier.Name,
                Title = CapabilityPluginNames.ResolveTitle(o.Title, notifier.Title),
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                ActivityCodes = o.ActivityCodes,
                TaskKinds = o.TaskKinds,
                Mode = o.Mode == HookMode.Sync ? HookMode.AsyncOutbox : o.Mode,
                Handler = notifier,
                InProcessThrottle = o.InProcessThrottle,
            };

        internal static CopytoNotifierRegistration BuildCopytoNotifierRegistration(
            ICopytoNotifier notifier,
            HookRegistrationOptions o) =>
            new()
            {
                Name = notifier.Name,
                Title = CapabilityPluginNames.ResolveTitle(o.Title, notifier.Title),
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                CopytoKinds = o.CopytoKinds,
                Mode = o.Mode == HookMode.Sync ? HookMode.AsyncOutbox : o.Mode,
                Handler = notifier,
            };

        internal static ProcessInstanceCreateHookRegistration BuildCreateHookRegistration(
            IProcessInstanceCreateHook hook,
            HookRegistrationOptions o) =>
            new()
            {
                Name = hook.Name,
                Title = CapabilityPluginNames.ResolveTitle(o.Title, hook.Title),
                Order = o.Order,
                Match = o.Match,
                ProcessCodes = o.ProcessCodes,
                ProcessIds = o.ProcessIds,
                BizChannels = o.BizChannels,
                BizTypes = o.BizTypes,
                Handler = hook,
                TimeoutMs = o.TimeoutMs,
            };

        private void TraceWarning(string message) =>
            Client.Log?.Warning("capability-registry", message);
    }
}
