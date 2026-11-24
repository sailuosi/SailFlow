using sailflow.assignee;

namespace sailflow.assignee.Builtin;

/// <summary>
/// 有效权重：只认委托链根；同根同意/拒绝各最多计一次根权。
/// Leave 实现禁止私自跟 ProxySignId 找根。
/// </summary>
public static class EffectiveWeightHelper
{
    public static LeaveSeatSnapshot? GetWeightRoot(
        IReadOnlyList<LeaveSeatSnapshot> seats,
        LeaveSeatSnapshot p)
    {
        if (p == null || seats == null || seats.Count == 0)
            return p;

        var bySign = seats
            .Where(s => !string.IsNullOrWhiteSpace(s?.SignId))
            .GroupBy(s => s.SignId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var guard = 0;
        var cur = p;
        while (!string.IsNullOrWhiteSpace(cur.ProxySignId) && guard++ < 32)
        {
            if (!bySign.TryGetValue(cur.ProxySignId.Trim(), out var parent)
                || ReferenceEquals(parent, cur)
                || string.Equals(parent.SignId, cur.SignId, StringComparison.OrdinalIgnoreCase))
                break;
            cur = parent;
        }

        return cur;
    }

    public static decimal EffectiveWeight(
        IReadOnlyList<LeaveSeatSnapshot> seats,
        LeaveSeatSnapshot p)
    {
        if (p == null || p.ClosedOrCancelled)
            return 0m;
        if (p.Weight <= 0)
            return 0m;
        var root = GetWeightRoot(seats, p);
        if (root == null || root.ClosedOrCancelled)
            return 0m;
        // 显式 Weight≤0 = 废席；未写/正数取根权（投影侧默认 1）
        return root.Weight <= 0 ? 0m : root.Weight;
    }

    /// <summary>分母：按根去重后的有效权之和（不含已关席；显式 Weight≤0 废席不入）。</summary>
    public static decimal SumDenominator(IReadOnlyList<LeaveSeatSnapshot> seats)
    {
        if (seats == null || seats.Count == 0)
            return 0m;

        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal sum = 0m;
        foreach (var p in seats)
        {
            if (p == null || p.ClosedOrCancelled)
                continue;
            var root = GetWeightRoot(seats, p);
            if (root == null || string.IsNullOrWhiteSpace(root.SignId))
                continue;
            if (!roots.Add(root.SignId))
                continue;
            if (root.Weight <= 0)
                continue;
            sum += root.Weight;
        }

        return sum;
    }

    public static decimal SumAgreedEffective(IReadOnlyList<LeaveSeatSnapshot> seats)
        => SumByFlag(seats, agreed: true);

    public static decimal SumRefusedEffective(IReadOnlyList<LeaveSeatSnapshot> seats)
        => SumByFlag(seats, agreed: false);

    static decimal SumByFlag(IReadOnlyList<LeaveSeatSnapshot> seats, bool agreed)
    {
        if (seats == null || seats.Count == 0)
            return 0m;

        var counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal sum = 0m;
        foreach (var p in seats)
        {
            if (p == null)
                continue;
            if (agreed)
            {
                if (!p.Agreed || p.ClosedOrCancelled)
                    continue;
            }
            else
            {
                if (!p.Refused)
                    continue;
            }

            var root = GetWeightRoot(seats, p);
            if (root == null || string.IsNullOrWhiteSpace(root.SignId))
                continue;
            if (!counted.Add(root.SignId))
                continue;
            if (root.Weight <= 0)
                continue;
            sum += root.Weight;
        }

        return sum;
    }
}
