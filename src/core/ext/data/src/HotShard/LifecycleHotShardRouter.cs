using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Client;
using sailflow.engine.HotShard;

namespace sailflow.data.HotShard
{
    /// <summary>
    /// 按 <see cref="SailDataLifecycleOptions.HotShardProcessIds"/> 白名单路由轨迹/变量物理表。
    /// </summary>
    public sealed class LifecycleHotShardRouter : ISailHotShardRouter
    {
        public const string TransitionLogicalBase = "tran_instance";
        public const string VariableLogicalBase = "proc_variable";

        private readonly HashSet<string> _whitelist;
        private readonly SailDataLifecycleOptions _options;
        private readonly IReadOnlyList<string> _transitionLookup;
        private readonly IReadOnlyList<string> _variableLookup;

        public LifecycleHotShardRouter(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            _options = client.DataLifecycleOptions ?? new SailDataLifecycleOptions();
            _whitelist = new HashSet<string>(
                (_options.HotShardProcessIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var transition = new List<string> { DefaultTransitionTable };
            var variable = new List<string> { DefaultVariableTable };
            foreach (var id in _whitelist)
            {
                var t = FormatTable(TransitionLogicalBase, id);
                var v = FormatTable(VariableLogicalBase, id);
                if (!transition.Contains(t, StringComparer.OrdinalIgnoreCase))
                    transition.Add(t);
                if (!variable.Contains(v, StringComparer.OrdinalIgnoreCase))
                    variable.Add(v);
            }

            _transitionLookup = transition;
            _variableLookup = variable;
        }

        public SailClient Client { get; }

        public string DefaultTransitionTable => "wf_tran_instance";
        public string DefaultVariableTable => "wf_proc_variable";

        public IReadOnlyList<string> TransitionLookupTables => _transitionLookup;
        public IReadOnlyList<string> VariableLookupTables => _variableLookup;

        public bool IsSharded(string processId)
            => !string.IsNullOrWhiteSpace(processId) && _whitelist.Contains(processId.Trim());

        public string ResolveTransitionTable(string processId)
            => IsSharded(processId)
                ? FormatTable(TransitionLogicalBase, processId)
                : DefaultTransitionTable;

        public string ResolveVariableTable(string processId)
            => IsSharded(processId)
                ? FormatTable(VariableLogicalBase, processId)
                : DefaultVariableTable;

        public string FormatTable(string logicalBase, string processId)
            => _options.FormatHotShardTableName(logicalBase, SanitizeSuffix(processId, _options.HotShardSuffixMaxLength));

        public static string SanitizeSuffix(string processId, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(processId))
                return "unknown";

            var sb = new StringBuilder(processId.Length);
            foreach (var ch in processId.Trim())
            {
                if ((ch >= '0' && ch <= '9')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= 'a' && ch <= 'z'))
                    sb.Append(ch);
                else
                    sb.Append('_');
            }

            var s = sb.ToString();
            if (s.Length == 0)
                s = "unknown";
            if (maxLength > 0 && s.Length > maxLength)
                s = s.Substring(0, maxLength);
            return s;
        }
    }
}
