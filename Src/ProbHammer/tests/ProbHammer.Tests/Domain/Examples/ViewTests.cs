using FluentAssertions;
using ProbHammer.Core.Domain.Examples;

namespace ProbHammer.Tests.Domain.Examples;

public class ViewTests
{
    [Fact]
    public void Roster_UnitsMatchesMyArmyRoster()
    {
        var rosterUnits = View.Roster().Units;
        var myArmyRoster = View.MyArmyRoster();

        rosterUnits.Should().HaveCount(9);
        rosterUnits.Select(u => u.Name).Should().Equal(myArmyRoster.Select(u => u.Name));
    }

    [Fact]
    public void Roster_MetadataMatchesSampleExport()
    {
        var roster = View.Roster();

        roster.Name.Should().Be("For the Emperor");
        roster.PointsSpent.Should().Be(1065);
        roster.Faction.Should().Equal("Space Marines", "Black Templars");
        roster.Detachments.Should().Equal("Companions of Vehemence");
        roster.ForceDisposition.Should().Be("Purge the Foe");
        roster.BattleSize.Should().Be("Incursion");
        roster.PointsLimit.Should().Be(1000);
        roster.PointsSpent.Should().NotBe(roster.PointsLimit);
    }
}
