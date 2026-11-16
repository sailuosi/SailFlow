using sailflow.engine.Common;
using Xunit;

namespace sailflow.engine.tests;

/// <summary>引擎 EndType 码表稳定；Host 文案映射另测。此处仅锁码字面量。</summary>
public class TaskEndTypesLocalizeSmokeTests
{
    [Fact]
    public void EndType_Codes_Are_Stable()
    {
        Assert.Equal("SelfComplete", TaskEndTypes.SelfComplete);
        Assert.Equal("ProxyTaken", TaskEndTypes.ProxyTaken);
        Assert.Equal("RaceTaken", TaskEndTypes.RaceTaken);
        Assert.Equal("DelegatedOut", TaskEndTypes.DelegatedOut);
        Assert.Equal("TransferredOut", TaskEndTypes.TransferredOut);
    }
}
