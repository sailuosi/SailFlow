using System;
using System.Linq;
using System.Reflection;
using mooSQL.data;
using saiflow.engine.Entity;
using Xunit;

namespace saiflow.engine.tests
{
    /// <summary>Guid 字符串主键 / AutoKey 冒烟（不落库）。</summary>
    public class GuidStringAutoKeyTests
    {
        [Fact]
        public void ProcessEntity_Id_Is_String_With_AutoKey_Guid()
        {
            var prop = typeof(ProcessEntity).GetProperty(nameof(ProcessEntity.ProcessDefId));
            Assert.NotNull(prop);
            Assert.Equal(typeof(string), prop.PropertyType);

            var col = prop.GetCustomAttributes(false)
                .Select(a => a.GetType().GetProperty("AutoKey")?.GetValue(a) as string)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            Assert.Equal("Guid", col);

            var isIdentity = prop.GetCustomAttributes(false)
                .Select(a => a.GetType().GetProperty("IsIdentity")?.GetValue(a) as bool?)
                .FirstOrDefault(b => b == true);
            Assert.Null(isIdentity);
        }

        [Fact]
        public void ResolveAutoKey_Guid_Yields_String_For_String_Property()
        {
            var client = new MooClient();
            var v = client.ResolveAutoKey("Guid", typeof(string));
            Assert.IsType<string>(v);
            Assert.True(Guid.TryParse((string)v, out _));
        }

        [Fact]
        public void ActivityInstance_Fk_ProcessInstanceId_Is_String()
        {
            Assert.Equal(typeof(string), typeof(ActivityInstanceEntity).GetProperty(nameof(ActivityInstanceEntity.ProcessInstanceId))!.PropertyType);
            Assert.Equal(typeof(string), typeof(TaskEntity).GetProperty(nameof(TaskEntity.ActivityInstanceId))!.PropertyType);
            Assert.Equal(typeof(string), typeof(TaskEntity).GetProperty(nameof(TaskEntity.ProcessInstanceId))!.PropertyType);
        }
    }
}
