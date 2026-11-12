using sailflow.engine.Hooks;
using Xunit;

namespace sailflow.engine.tests;

public sealed class TaskNotifyMessageIntentMappingTests
{
    [Theory]
    [InlineData(TaskNotifyKind.Created, TaskNotifyMessageIntent.Open)]
    [InlineData(TaskNotifyKind.Entrusted, TaskNotifyMessageIntent.Open)]
    [InlineData(TaskNotifyKind.Completed, TaskNotifyMessageIntent.Close)]
    [InlineData(TaskNotifyKind.Cancelled, TaskNotifyMessageIntent.Close)]
    public void FromKind_maps_lifecycle(TaskNotifyKind kind, TaskNotifyMessageIntent expected)
    {
        Assert.Equal(expected, TaskNotifyMessageIntentMapping.FromKind(kind));
    }
}
