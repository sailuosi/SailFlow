using System.Collections.Generic;

namespace saiflow.engine.HotShard
{
    /// <summary>
    /// 轨迹 / 变量热表白名单分表路由。默认实现恒返回默认表名；白名单实现由 saiflow.data 挂载。
    /// </summary>
    public interface ISailHotShardRouter
    {
        string DefaultTransitionTable { get; }
        string DefaultVariableTable { get; }

        bool IsSharded(string processId);

        string ResolveTransitionTable(string processId);

        string ResolveVariableTable(string processId);

        /// <summary>GetById 探测顺序：默认表 + 各白名单分表。</summary>
        IReadOnlyList<string> TransitionLookupTables { get; }

        /// <summary>GetById 探测顺序：默认表 + 各白名单分表。</summary>
        IReadOnlyList<string> VariableLookupTables { get; }
    }

    /// <summary>无分表：始终使用默认物理表。</summary>
    public sealed class NullSailHotShardRouter : ISailHotShardRouter
    {
        public static readonly NullSailHotShardRouter Instance = new NullSailHotShardRouter();

        public string DefaultTransitionTable => "wf_transition_instance";
        public string DefaultVariableTable => "wf_process_variable";

        public bool IsSharded(string processId) => false;

        public string ResolveTransitionTable(string processId) => DefaultTransitionTable;

        public string ResolveVariableTable(string processId) => DefaultVariableTable;

        public IReadOnlyList<string> TransitionLookupTables { get; } = new[] { "wf_transition_instance" };

        public IReadOnlyList<string> VariableLookupTables { get; } = new[] { "wf_process_variable" };
    }
}
