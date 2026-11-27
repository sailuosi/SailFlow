using System.Collections.Generic;
using sailflow.data;
using sailflow.utils;
using Xunit;

namespace sailflow.engine.tests;

public class TakeOnPeerHelperTests
{
    [Theory]
    [InlineData("Race", true)]
    [InlineData("GroupRace", true)]
    [InlineData("race", true)]
    [InlineData("Collaborate", false)]
    [InlineData("GroupCollaborate", false)]
    [InlineData("Captain", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsTakeOnLeaveAllowed_Matches_Race_Family(string leave, bool expected)
    {
        Assert.Equal(expected, TakeOnPeerHelper.IsTakeOnLeaveAllowed(leave));
    }

    [Fact]
    public void FilterPeersToClose_Race_Closes_All_Others_Keeps_Own_Seat()
    {
        var open = new List<TaskEntity>
        {
            Task("t1", "zhang", null, null),
            Task("t2", "li", null, null),
            Task("t3", "wang", null, null),
            Task("t4", "proxy", "zhang", null), // 同根代批
        };

        var close = TakeOnPeerHelper.FilterPeersToClose(
            open, "t1", "zhang", null, null, "Race");

        Assert.Equal(2, close.Count);
        Assert.Contains(close, t => t.TaskId == "t2");
        Assert.Contains(close, t => t.TaskId == "t3");
        Assert.DoesNotContain(close, t => t.TaskId == "t1");
        Assert.DoesNotContain(close, t => t.TaskId == "t4");
    }

    [Fact]
    public void FilterPeersToClose_GroupRace_Only_Same_Group()
    {
        var open = new List<TaskEntity>
        {
            Task("t1", "a1", null, "g1"),
            Task("t2", "a2", null, "g1"),
            Task("t3", "b1", null, "g2"),
        };

        var close = TakeOnPeerHelper.FilterPeersToClose(
            open, "t1", "a1", null, "g1", "GroupRace");

        Assert.Single(close);
        Assert.Equal("t2", close[0].TaskId);
    }

    [Fact]
    public void FilterPeersToClose_GroupRace_Empty_Group_Closes_All_Others()
    {
        var open = new List<TaskEntity>
        {
            Task("t1", "a1", null, null),
            Task("t2", "a2", null, "g1"),
            Task("t3", "b1", null, "g2"),
        };

        var close = TakeOnPeerHelper.FilterPeersToClose(
            open, "t1", "a1", null, null, "GroupRace");

        Assert.Equal(2, close.Count);
    }

    [Fact]
    public void FilterPeersToClose_Already_Sole_Is_Empty()
    {
        var open = new List<TaskEntity>
        {
            Task("t1", "zhang", null, null),
        };

        var close = TakeOnPeerHelper.FilterPeersToClose(
            open, "t1", "zhang", null, null, "Race");

        Assert.Empty(close);
    }

    [Fact]
    public void EndType_TakeOnTaken_Is_Stable()
    {
        Assert.Equal("TakeOnTaken", TaskEndTypes.TakeOnTaken);
    }

    static TaskEntity Task(string id, string sign, string proxy, string group) => new()
    {
        TaskId = id,
        SignId = sign,
        ProxySignId = proxy,
        GroupId = group,
        TaskState = (short)TaskStateEnum.Waiting
    };
}
