using FluentAssertions;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

public class ModelLineTests
{
    [Fact]
    public void RemainingCount_IsInitializedFromOriginalCount()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);

        modelLine.RemainingCount.Should().Be(5);
    }

    [Fact]
    public void RemovingCasualties_DecrementsRemainingCount()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);

        modelLine.RemoveCasualties(2);

        modelLine.RemainingCount.Should().Be(3);
    }

    [Fact]
    public void RemovingASingleModelLine_ReachesZero()
    {
        var modelLine = new ModelLine("Chaplain", [], count: 1);

        modelLine.RemoveCasualties(1);

        modelLine.RemainingCount.Should().Be(0);
    }

    [Fact]
    public void RemainingCount_CannotGoNegative()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);
        modelLine.RemoveCasualties(5);

        modelLine.RemoveCasualties(1);

        modelLine.RemainingCount.Should().Be(0);
    }
}
