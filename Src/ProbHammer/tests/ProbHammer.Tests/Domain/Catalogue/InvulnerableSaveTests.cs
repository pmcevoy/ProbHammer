using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class InvulnerableSaveTests
{
    [Fact]
    public void Absent_HasZeroMeleeAndRangedValues()
    {
        var save = new InvulnerableSave(0, 0);

        save.MeleeInSv.Should().Be(0);
        save.RangedInSv.Should().Be(0);
    }

    [Fact]
    public void Uniform_HasEqualMeleeAndRangedValues()
    {
        var save = new InvulnerableSave(4, 4);

        save.MeleeInSv.Should().Be(4);
        save.RangedInSv.Should().Be(4);
    }

    [Fact]
    public void ImplicitIntConversion_ProducesUniformValue()
    {
        InvulnerableSave save = 5;

        save.MeleeInSv.Should().Be(5);
        save.RangedInSv.Should().Be(5);
    }

    [Fact]
    public void None_IsTheAbsentValue()
    {
        InvulnerableSave.None.Should().Be(new InvulnerableSave(0, 0));
    }
}