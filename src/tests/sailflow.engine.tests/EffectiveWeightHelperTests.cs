using sailflow.assignee.Builtin;
using sailflow.engine.Assignee;
using Xunit;

namespace sailflow.engine.tests;

public class EffectiveWeightHelperTests
{
    static LeaveSeatSnapshot Seat(
        string signId,
        decimal weight = 1m,
        string? proxy = null,
        bool agreed = false,
        bool refused = false,
        bool closed = false) =>
        new()
        {
            SignId = signId,
            Weight = weight,
            ProxySignId = proxy,
            Agreed = agreed,
            Refused = refused,
            ClosedOrCancelled = closed
        };

    [Fact]
    public void Proxy_Does_Not_Inflate_Denominator()
    {
        var seats = new List<LeaveSeatSnapshot>
        {
            Seat("李四", 1m),
            Seat("张三", 1m, proxy: "李四")
        };

        Assert.Equal(1m, EffectiveWeightHelper.SumDenominator(seats));
    }

    [Fact]
    public void Dual_Agree_Same_Root_Counts_Once()
    {
        var seats = new List<LeaveSeatSnapshot>
        {
            Seat("李四", 1m, agreed: true),
            Seat("张三", 1m, proxy: "李四", agreed: true)
        };

        Assert.Equal(1m, EffectiveWeightHelper.SumAgreedEffective(seats));
        Assert.Equal(1m, EffectiveWeightHelper.SumDenominator(seats));
    }

    [Fact]
    public void Dual_Refuse_Same_Root_Counts_Once()
    {
        var seats = new List<LeaveSeatSnapshot>
        {
            Seat("李四", 2m, refused: true),
            Seat("张三", 1m, proxy: "李四", refused: true)
        };

        Assert.Equal(2m, EffectiveWeightHelper.SumRefusedEffective(seats));
    }

    [Fact]
    public void Closed_Proxy_Still_Allows_Root_Agree_Via_Agent()
    {
        // 原人被 ProxyTaken 关闭后，代理人 Completed 同意仍计根权
        var seats = new List<LeaveSeatSnapshot>
        {
            Seat("李四", 1m, closed: true),
            Seat("张三", 1m, proxy: "李四", agreed: true)
        };

        Assert.Equal(1m, EffectiveWeightHelper.SumAgreedEffective(seats));
        Assert.Equal(1m, EffectiveWeightHelper.SumDenominator(seats));
    }

    [Fact]
    public void Default_Weight_One_When_Missing()
    {
        var seats = new List<LeaveSeatSnapshot>
        {
            Seat("A", 0m), // Weight≤0 → 有效 0，不入分母
            Seat("B", 1m)
        };

        Assert.Equal(1m, EffectiveWeightHelper.SumDenominator(seats));
    }
}
