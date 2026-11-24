using sailflow.data;
using sailflow.engine.Event;
using Xunit;

namespace sailflow.engine.tests;

/// <summary>待办级 SLA：实体字段与 OnActivityReady 事件码。</summary>
public class TaskTimeoutFieldsTests
{
    [Fact]
    public void TaskEntity_HasActivatedAndOverdue()
    {
        var task = new TaskEntity
        {
            ActiveAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddMinutes(10)
        };
        Assert.NotNull(task.ActiveAt);
        Assert.NotNull(task.DueAt);
        Assert.True(task.DueAt > task.ActiveAt);
    }

    [Fact]
    public void TaskViewEntity_HasActivatedAndOverdue()
    {
        var view = new TaskViewEntity
        {
            ActiveAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddHours(1)
        };
        Assert.NotNull(view.ActiveAt);
        Assert.NotNull(view.DueAt);
    }

    [Fact]
    public void OnActivityReady_IsDefined()
    {
        Assert.Equal(44, (int)EventFireTypeEnum.OnActivityReady);
    }
}
