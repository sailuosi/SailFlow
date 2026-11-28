using sailflow.data;
using sailflow.utils;
using Xunit;

namespace sailflow.engine.tests;

public class RowParaHelperTests
{
    [Fact]
    public void Parse_and_Format_roundtrip()
    {
        var s = RowParaHelper.SetKey(null, RowParaHelper.RemindWayKey, "sms,mail");
        s = RowParaHelper.SetKey(s, "foo", "bar");
        var map = RowParaHelper.Parse(s);
        Assert.Equal("sms,mail", map[RowParaHelper.RemindWayKey]);
        Assert.Equal("bar", map["foo"]);
    }

    [Fact]
    public void Merge_overlay_wins_same_key()
    {
        var basePara = RowParaHelper.SetKey(null, "a", "1");
        basePara = RowParaHelper.SetKey(basePara, "b", "2");
        var overlay = RowParaHelper.SetKey(null, "b", "9");
        var merged = RowParaHelper.Merge(basePara, overlay);
        var map = RowParaHelper.Parse(merged);
        Assert.Equal("1", map["a"]);
        Assert.Equal("9", map["b"]);
    }

    [Fact]
    public void TruncateTips_limits_500()
    {
        var longTips = new string('x', 600);
        var t = RowParaHelper.TruncateTips(longTips);
        Assert.Equal(500, t!.Length);
    }

    [Fact]
    public void ApplyRunnerTaskMeta_from_context()
    {
        var entity = new TaskEntity();
        var context = new FlowCommandContext
        {
            Hurry = 3,
            Tips = "请尽快办理",
            RowPara = RowParaHelper.SetKey(null, RowParaHelper.RemindWayKey, "sms")
        };
        TaskManager.ApplyRunnerTaskMeta(entity, context);
        Assert.Equal(3, entity.Hurry);
        Assert.Equal("请尽快办理", entity.Tips);
        Assert.Contains("remindWay=", entity.RowPara);
    }

    [Fact]
    public void ApplyRunnerTaskMeta_copy_then_merge_rowpara()
    {
        var src = new TaskEntity
        {
            Hurry = 1,
            Tips = "旧提示",
            RowPara = RowParaHelper.SetKey(null, "keep", "1")
        };
        var entity = new TaskEntity();
        var context = new FlowCommandContext
        {
            Hurry = 4,
            RowPara = RowParaHelper.SetKey(null, RowParaHelper.RemindWayKey, "app")
        };
        TaskManager.ApplyRunnerTaskMeta(entity, context, src);
        Assert.Equal(4, entity.Hurry);
        Assert.Equal("旧提示", entity.Tips);
        var map = RowParaHelper.Parse(entity.RowPara);
        Assert.Equal("1", map["keep"]);
        Assert.Equal("app", map[RowParaHelper.RemindWayKey]);
    }
}
