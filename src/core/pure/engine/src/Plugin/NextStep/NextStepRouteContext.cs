using System.Collections.Generic;
using sailflow.engine.Client;
using sailflow.engine.Common;

namespace sailflow.engine.NextStep;

/// <summary>下一跳管道运行时上下文（Prepare / Filter）。</summary>
public sealed class NextStepRouteContext
{
    public SailClient Sail { get; set; }

    /// <summary>可空：纯 processId 预览。</summary>
    public FlowCommandContext Runner { get; set; }

    public string ProcessId { get; set; }
    public string ProcessCode { get; set; }
    public string Version { get; set; }
    public string CurrentActivityId { get; set; }
    public string CurrentActivityCode { get; set; }
    public string ActivityInstanceId { get; set; }
    public string ProcessInstanceId { get; set; }
    public string BizPK { get; set; }
    public string UserId { get; set; }

    /// <summary>可写条件袋（Prepare）；Filter 建议只读。</summary>
    public IDictionary<string, string> Conditions { get; set; }

    public NextStepRoutePhase Phase { get; set; }
}
