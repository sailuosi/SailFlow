using saiflow.engine.Entity;
using saiflow.engine.Event;
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
            ActivatedDateTime = DateTime.UtcNow,
            OverdueDateTime = DateTime.UtcNow.AddMinutes(10)
        };
        Assert.NotNull(task.ActivatedDateTime);
        Assert.NotNull(task.OverdueDateTime);
        Assert.True(task.OverdueDateTime > task.ActivatedDateTime);
    }

    [Fact]
    public void TaskViewEntity_HasActivatedAndOverdue()
    {
        var view = new TaskViewEntity
        {
            ActivatedDateTime = DateTime.UtcNow,
            OverdueDateTime = DateTime.UtcNow.AddHours(1)
        };
        Assert.NotNull(view.ActivatedDateTime);
        Assert.NotNull(view.OverdueDateTime);
    }

    [Fact]
    public void OnActivityReady_IsDefined()
    {
        Assert.Equal(44, (int)EventFireTypeEnum.OnActivityReady);
    }
}
