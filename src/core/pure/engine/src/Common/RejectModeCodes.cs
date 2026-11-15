using System;

namespace sailflow.engine.Common;

/// <summary>
/// 驳回落点模式（string 码；由 Host 写入 <see cref="ControlParameterSheet.RejectMode"/>）。
/// 办理 UX 不选目标/不选人；引擎按模式自动解析。
/// </summary>
public static class RejectModeCodes
{
    /// <summary>终止流程实例（Lifecycle Terminate；≠ Discard 作废）。</summary>
    public const string Terminate = "Terminate";

    /// <summary>按执行链路回上一办理节点（默认）。</summary>
    public const string Previous = "Previous";

    /// <summary>回发起/首办节点。</summary>
    public const string Starter = "Starter";

    /// <summary>回指定历史节点（须配 <see cref="ControlParameterSheet.RejectReturnActivityId"/>）。</summary>
    public const string Specified = "Specified";

    /// <summary>缺省 / 空白 → Previous；已知码返回规范大小写；未知 → 空串。</summary>
    public static string Normalize(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return Previous;
        var m = mode.Trim();
        if (Equals(m, Terminate)) return Terminate;
        if (Equals(m, Previous)) return Previous;
        if (Equals(m, Starter)) return Starter;
        if (Equals(m, Specified)) return Specified;
        return string.Empty; // 非法：由调用方报 RejectModeInvalid
    }

    static bool Equals(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
