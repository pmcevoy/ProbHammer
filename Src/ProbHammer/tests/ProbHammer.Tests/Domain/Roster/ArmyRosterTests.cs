using FluentAssertions;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

public class ArmyRosterTests
{
    [Fact]
    public void SingleFaction_PreservesExactlyOneEntry()
    {
        var roster = new ArmyRoster(
            name: "YO HO HO",
            pointsSpent: 700,
            faction: ["Chaos Space Marines"],
            detachments: ["Cabal of Chaos", "Devotees of Destruction"],
            forceDisposition: "Priority Assets",
            battleSize: "Incursion",
            pointsLimit: 1000,
            units: []);

        roster.Faction.Should().ContainSingle().Which.Should().Be("Chaos Space Marines");
    }

    [Fact]
    public void ThreeDetachments_PreservesAllThreeInOrder()
    {
        var roster = new ArmyRoster(
            name: "Crusade with me",
            pointsSpent: 575,
            faction: ["Space Marines", "Black Templars"],
            detachments: ["Fulguris Task Force", "Marshal's Household", "Subversion Assets"],
            forceDisposition: "Reconnaissance",
            battleSize: "Strike Force",
            pointsLimit: 2000,
            units: []);

        roster.Detachments.Should().Equal("Fulguris Task Force", "Marshal's Household", "Subversion Assets");
    }
}
