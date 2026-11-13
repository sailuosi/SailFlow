using System;
using sailflow.engine.NextStep;

namespace sailflow.external.NextStep;

/// <summary>
/// 文档样例：按用户权限隐藏某候选支路（演示 Disabled + ReasonCode）。
/// AndSplit 必达支会被管道 Guard 恢复，不可依赖本 Filter 静默删并行支。
/// 注册：
/// <c>UseNextStepRoute(r => r.AddFilter&lt;SamplePermissionHideBranchFilter&gt;(o => o.ProcessCodes = new[] { "EXPENSE" }))</c>
/// </summary>
public sealed class SamplePermissionHideBranchFilter : INextStepFilter
{
    /// <summary>要隐藏的 ActivityCode（忽略大小写）。</summary>
    public string HideActivityCode { get; set; } = "finance_approve";

    public string ReasonCode { get; set; } = "NoPermission";

    public bool IsMatch(NextStepRouteContext ctx) => true;

    public void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates)
    {
        if (candidates?.Items == null || string.IsNullOrEmpty(HideActivityCode))
            return;

        foreach (var c in candidates.Items)
        {
            var code = c.Activity?.ActivityCode;
            if (string.IsNullOrEmpty(code))
                continue;
            if (!string.Equals(code, HideActivityCode, StringComparison.OrdinalIgnoreCase))
                continue;
            c.Disabled = true;
            if (string.IsNullOrEmpty(c.ReasonCode))
                c.ReasonCode = ReasonCode;
        }
    }
}
