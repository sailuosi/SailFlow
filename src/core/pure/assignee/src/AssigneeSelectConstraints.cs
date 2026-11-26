using sailflow.run;

namespace sailflow.assignee;

/// <summary>
/// 从 ConfigPair.Params 读取选人人数 / 预选写入授权（专题-对象解析器 §2.4）。
/// </summary>
public static class AssigneeSelectConstraints
{
    public const string StartWriterCode = "Start";

    public const string KeyAllowPreselectFrom = "allowPreselectFrom";
    public const string KeyMinSelect = "minSelect";
    public const string KeyMaxSelect = "maxSelect";

    /// <summary>Preselect 空仓回落：Reject（默认）| Input。</summary>
    public const string KeyPreselectFallback = "preselectFallback";

    public const string PreselectFallbackReject = "Reject";
    public const string PreselectFallbackInput = "Input";

    /// <summary>Pick 是否落库（LoadPicked/StorePicked）；默认 false。</summary>
    public const string KeyPersistPick = "persistPick";

    /// <summary>Pick/Preselect 默认下限；Fixed 不适用。</summary>
    public const int DefaultMinSelect = 1;

    /// <summary>
    /// Preselect Load 为空时行为。未配 / 非法 → <see cref="PreselectFallbackReject"/>。
    /// </summary>
    public static string GetPreselectFallback(ConfigPair? config)
    {
        var raw = ConfigPairParams.GetString(config, KeyPreselectFallback)?.Trim();
        if (string.Equals(raw, PreselectFallbackInput, StringComparison.OrdinalIgnoreCase))
            return PreselectFallbackInput;
        return PreselectFallbackReject;
    }

    public static bool IsPreselectFallbackInput(ConfigPair? config) =>
        string.Equals(
            GetPreselectFallback(config),
            PreselectFallbackInput,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Pick 持久化开关；默认关（不落库）。</summary>
    public static bool IsPersistPick(ConfigPair? config) =>
        ConfigPairParams.GetBool(config, KeyPersistPick, false);

    public static int GetMinSelect(ConfigPair? config)
    {
        if (config == null)
            return DefaultMinSelect;
        var n = ConfigPairParams.GetInt(config, KeyMinSelect);
        if (n == null)
        {
            // 旧键 required≈minSelect≥1
            if (ConfigPairParams.GetBool(config, "required", false))
                return 1;
            return DefaultMinSelect;
        }

        return n.Value < 0 ? 0 : n.Value;
    }

    /// <summary>null = 不限制上限。</summary>
    public static int? GetMaxSelect(ConfigPair? config)
    {
        if (config == null)
            return null;
        var n = ConfigPairParams.GetInt(config, KeyMaxSelect);
        if (n != null)
            return n.Value < 0 ? null : n;

        // 旧键 allowMulti=false → maxSelect=1
        if (config.Params != null
            && config.Params.ContainsKey("allowMulti")
            && !ConfigPairParams.GetBool(config, "allowMulti", true))
            return 1;

        return null;
    }

    /// <summary>
    /// 未配或空列表 → 仅 <see cref="StartWriterCode"/>。
    /// </summary>
    public static IReadOnlyList<string> GetAllowPreselectFrom(ConfigPair? config)
    {
        if (config == null)
            return new[] { StartWriterCode };
        var arr = ConfigPairParams.GetStringArray(config, KeyAllowPreselectFrom);
        if (arr.Length == 0)
            return new[] { StartWriterCode };
        return arr
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool IsWriterAllowed(ConfigPair? config, string? writerActivityId)
    {
        if (string.IsNullOrWhiteSpace(writerActivityId))
            return false;
        var w = writerActivityId.Trim();
        var allow = GetAllowPreselectFrom(config);
        return allow.Any(a => string.Equals(a, w, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 校验人数；不通过抛 <see cref="WfAssigneeException"/>。
    /// Fixed 调用方勿用。
    /// </summary>
    public static void EnsureSelectCount(ConfigPair? config, int count, string context)
    {
        var min = GetMinSelect(config);
        var max = GetMaxSelect(config);
        if (count < min)
        {
            throw new WfAssigneeException(
                AssigneeReasonCodes.SelectCountBelowMin,
                context
                + ": select count "
                + count
                + " < minSelect "
                + min
                + ". ReasonCode="
                + AssigneeReasonCodes.SelectCountBelowMin);
        }

        if (max != null && count > max.Value)
        {
            throw new WfAssigneeException(
                AssigneeReasonCodes.SelectCountAboveMax,
                context
                + ": select count "
                + count
                + " > maxSelect "
                + max.Value
                + ". ReasonCode="
                + AssigneeReasonCodes.SelectCountAboveMax);
        }
    }

    public static void EnsureWriterAllowed(ConfigPair? config, string? writerActivityId)
    {
        if (IsWriterAllowed(config, writerActivityId))
            return;
        throw new WfAssigneeException(
            AssigneeReasonCodes.PreselectWriterNotAllowed,
            "Preselect writer '"
            + (writerActivityId ?? "")
            + "' is not in allowPreselectFrom. ReasonCode="
            + AssigneeReasonCodes.PreselectWriterNotAllowed);
    }
}
