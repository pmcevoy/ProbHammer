using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class NameResolutionTests
{
    [Fact]
    public void Unit_NameIsItsDatasheetName()
    {
        var unit = UnitFixtures.CrusaderSquadMixedLoadout();

        unit.Name.Should().Be("Crusader Squad");
    }

    [Fact]
    public void AttachedUnit_WithNoAttachedUnits_NameIsBodyguardNameAlone()
    {
        var attachedUnit = new AttachedUnit(UnitFixtures.CrusaderSquadMixedLoadout(), []);

        attachedUnit.Name.Should().Be("Crusader Squad");
    }

    [Fact]
    public void AttachedUnit_WithOneAttachedUnit_NameJoinsWithSingleWith()
    {
        var attachedUnit = new AttachedUnit(
            UnitFixtures.CrusaderSquadMixedLoadout(),
            [AttachedUnitFixtures.LeaderUnit("Marshal")]);

        attachedUnit.Name.Should().Be("Crusader Squad with Marshal");
    }

    [Fact]
    public void AttachedUnit_WithTwoAttachedUnits_NameJoinsWithAndAndNoComma()
    {
        var attachedUnit = new AttachedUnit(
            UnitFixtures.CrusaderSquadMixedLoadout(),
            [AttachedUnitFixtures.LeaderUnit("Marshal"), AttachedUnitFixtures.LeaderUnit("Lieutenant")]);

        attachedUnit.Name.Should().Be("Crusader Squad with Marshal and Lieutenant");
    }

    [Fact]
    public void AttachedUnit_WithThreeOrMoreAttachedUnits_NameUsesOxfordComma()
    {
        var attachedUnit = AttachedUnitFixtures.TwoLeadersPlusSupport();

        attachedUnit.Name.Should().Be("Crusader Squad with Chaplain, Judiciar, and Servitor");
    }

    [Fact]
    public void AttachedUnit_WithDuplicateAttachedNames_ListsTheNameTwiceUnmerged()
    {
        var attachedUnit = new AttachedUnit(
            UnitFixtures.CrusaderSquadMixedLoadout(),
            [AttachedUnitFixtures.LeaderUnit("Marshal"), AttachedUnitFixtures.LeaderUnit("Marshal")]);

        attachedUnit.Name.Should().Be("Crusader Squad with Marshal and Marshal");
    }

    [Fact]
    public void Name_IsRecomputedNotCached_AcrossRepeatedReads()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        var firstRead = attachedUnit.Name;
        var secondRead = attachedUnit.Name;

        secondRead.Should().Be(firstRead);
    }
}
