using System;
using System.Collections.Generic;
using System.Linq;

namespace sailflow.engine.Rules
{
    /// <summary>
    /// 规则引擎注册表（按 mode 字符串调度）。
    /// </summary>
    public sealed class RuleEngineRegistry
    {
        private readonly Dictionary<string, IRuleEngine> _engines =
            new Dictionary<string, IRuleEngine>(StringComparer.OrdinalIgnoreCase);

        /// <summary>已注册的 mode 列表。</summary>
        public IReadOnlyCollection<string> Modes => _engines.Keys.ToList();

        /// <summary>注册或覆盖某一 mode 的引擎。</summary>
        public RuleEngineRegistry Register(IRuleEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (string.IsNullOrWhiteSpace(engine.Mode))
                throw new ArgumentException("IRuleEngine.Mode is required.", nameof(engine));

            _engines[engine.Mode.Trim()] = engine;
            return this;
        }

        public bool TryGet(string mode, out IRuleEngine engine)
        {
            engine = null;
            if (string.IsNullOrWhiteSpace(mode))
                return false;
            return _engines.TryGetValue(mode.Trim(), out engine);
        }

        public IRuleEngine GetRequired(string mode)
        {
            if (TryGet(mode, out var engine))
                return engine;

            var known = _engines.Count == 0
                ? "(none registered)"
                : string.Join(", ", _engines.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(
                "No rule engine registered for mode \"" + mode + "\". Registered: " + known +
                ". Call client.useRuleEngine(...) or builder.UseBizRuleEngines().");
        }
    }
}
