using System.Collections.Generic;
using mooSQL.data;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;

namespace sailflow.biz.Approval;

/// <summary>审批业务门面上下文（宿主发起模板可 <see cref="ToRunner"/>）。</summary>
public class SailApprovalBusinessContext
{
    public string FormCode { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string ProcessId { get; set; } = "";
    public string Version { get; set; } = "1";
    public string BizChannel { get; set; } = "";
    public string BizPK { get; set; } = "";
    public string? BizType { get; set; }
    public string? BizCode { get; set; }
    public string? BizName { get; set; }
    public string ProcessInstanceId { get; set; } = "";
    public Dictionary<string, string> Conditions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>发起/查询 JSON（Submit 入参）。</summary>
    public string? PayloadJson { get; set; }

    /// <summary>Handler 可挂业务实体等。</summary>
    public object? Data { get; set; }

    public string? ButtonId { get; set; }

    /// <summary>触发终态的引擎事件（可选）。</summary>
    public EventFireTypeEnum? FinishEvent { get; set; }

    /// <summary>OnFinish 结论：<see cref="ApprovalOutcomeCodes"/>。</summary>
    public string? Outcome { get; set; }

    /// <summary>OnAborted 种类。</summary>
    public ApprovalAbortKind? AbortKind { get; set; }

    /// <summary>Gate Completing 时的操作。</summary>
    public FlowOperation? CompletingOperation { get; set; }

    public string? Error { get; set; }

    /// <summary>
    /// Sync Hook 同事务 kit（来自 <c>FlowHookContext.Kit</c>）。
    /// 回写业务表时必须用此连接，勿另开 <c>UseRunSQL</c>。
    /// </summary>
    public SQLBuilder? Kit { get; set; }

    /// <summary>发起首跳办理人；空则 Host 可按首个人工节点默认填当前用户。</summary>
    public IDictionary<string, PerformerList>? NextActivityPerformers { get; set; }

    public FlowCommandContext ToRunner()
    {
        var context = new FlowCommandContext
        {
            UserId = UserId,
            UserName = UserName,
            ProcessId = ProcessId,
            Version = string.IsNullOrWhiteSpace(Version) ? "1" : Version,
            Conditions = Conditions.Count > 0 ? new Dictionary<string, string>(Conditions) : null,
            NextActivityPerformers = NextActivityPerformers
        };
        context.ApplyBizData(new BizDataDto
        {
            BizPK = BizPK,
            BizType = BizType,
            BizCode = BizCode,
            BizChannel = string.IsNullOrWhiteSpace(BizChannel) ? FormCode : BizChannel,
            BizName = BizName
        });
        return context;
    }
}
