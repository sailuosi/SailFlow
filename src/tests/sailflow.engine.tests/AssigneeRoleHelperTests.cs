using sailflow.utils;
using Xunit;

namespace sailflow.engine.tests;

public class AssigneeRoleHelperTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void IsApprover_int(int? role, bool expected)
        => Assert.Equal(expected, AssigneeRoleHelper.IsApprover(role));

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IsParticipant_int(int? role, bool expected)
        => Assert.Equal(expected, AssigneeRoleHelper.IsParticipant(role));

    [Fact]
    public void Participant_enum_value_is_one()
        => Assert.Equal(1, (int)AssigneeRoleEnum.Participant);
}
