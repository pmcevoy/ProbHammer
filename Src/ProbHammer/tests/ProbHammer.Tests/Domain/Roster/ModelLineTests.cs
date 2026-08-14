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

    [Fact]
    public void SetRemainingCount_IncreasesRemainingCount_ToCorrectAMistakenRemoval()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);
        modelLine.RemoveCasualties(2);

        modelLine.SetRemainingCount(4);

        modelLine.RemainingCount.Should().Be(4);
    }

    [Fact]
    public void SetRemainingCount_ClampsAtTheOriginalCount()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);

        modelLine.SetRemainingCount(99);

        modelLine.RemainingCount.Should().Be(5);
    }

    [Fact]
    public void SetRemainingCount_ClampsAtZero()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);

        modelLine.SetRemainingCount(-1);

        modelLine.RemainingCount.Should().Be(0);
    }

    [Fact]
    public void SetRemainingCount_ARoundTrip_ReturnsToTheStartingCount()
    {
        var modelLine = new ModelLine("Initiate", ["Bolt Rifle"], count: 5);

        modelLine.SetRemainingCount(2);
        modelLine.SetRemainingCount(5);

        modelLine.RemainingCount.Should().Be(5);
    }
}
