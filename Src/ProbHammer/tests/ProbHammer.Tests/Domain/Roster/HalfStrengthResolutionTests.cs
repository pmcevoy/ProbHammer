using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class HalfStrengthResolutionTests
{
    [Fact]
    public void StartingStrength_SumsModelLineCountsAcrossAllModelLines()
    {
        // Assault Intercessor Sergeant (1) + Assault Intercessor (4) = 5.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();

        HalfStrengthResolution.StartingStrength(unit).Should().Be(5);
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_TenModelUnit_TrueAtExactlyHalf()
    {
        var unit = new Unit(
            AttachedUnitFixtures.SupportDatasheet(), enhancements: [], modelLines: [new ModelLine("Servitor", [], count: 10)]);

        unit.ModelLines[0].SetRemainingCount(5);

        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeTrue(); // 5 <= ceil(10/2) = 5
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_TenModelUnit_FalseJustAboveHalf()
    {
        var unit = new Unit(
            AttachedUnitFixtures.SupportDatasheet(), enhancements: [], modelLines: [new ModelLine("Servitor", [], count: 10)]);

        unit.ModelLines[0].SetRemainingCount(6);

        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeFalse(); // 6 > ceil(10/2) = 5
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_OddStartingStrength_RoundsThresholdDown()
    {
        // 5-model unit: half = floor(5/2) = 2 - it takes 3 casualties, not 2, to reach it.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var trooperLine = unit.ModelLines.Single(ml => ml.StatlineName == "Assault Intercessor");

        trooperLine.SetRemainingCount(2); // sergeant (1) + 2 troopers = 3 remaining
        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeFalse(); // 3 > 2

        trooperLine.SetRemainingCount(1); // sergeant (1) + 1 trooper = 2 remaining
        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeTrue(); // 2 <= 2
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_ThreeModelUnit_FalseAfterOnlyOneCasualty_TrueAfterTwo()
    {
        // half = floor(3/2) = 1 - a single casualty (remaining 2) is not enough.
        var unit = new Unit(
            AttachedUnitFixtures.SupportDatasheet(), enhancements: [], modelLines: [new ModelLine("Servitor", [], count: 3)]);

        unit.ModelLines[0].RemoveCasualties(1); // 3 -> 2
        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeFalse(); // 2 > 1

        unit.ModelLines[0].RemoveCasualties(1); // 2 -> 1
        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeTrue(); // 1 <= 1
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_SingleModelUnit_AlwaysFalse()
    {
        // The computed determination never applies to a combined starting strength of 1 - see
        // Single-Model Half-Strength Is Player-Set.
        var unit = AttachedUnitFixtures.LeaderUnit();

        HalfStrengthResolution.IsAtOrBelowHalfStrength(unit).Should().BeFalse();
    }

    [Fact]
    public void IsAtOrBelowHalfStrengthStatus_SingleModelUnit_ReturnsThePlayerSetOverride()
    {
        var unit = AttachedUnitFixtures.LeaderUnit();

        HalfStrengthResolution.IsAtOrBelowHalfStrengthStatus(unit).Should().BeFalse();

        unit.IsHalfStrengthOverride = true;
        HalfStrengthResolution.IsAtOrBelowHalfStrengthStatus(unit).Should().BeTrue();
    }

    [Fact]
    public void IsAtOrBelowHalfStrengthStatus_MultiModelUnit_IgnoresThePlayerSetOverride()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform(); // 4 full-health models
        unit.IsHalfStrengthOverride = true;

        HalfStrengthResolution.IsAtOrBelowHalfStrengthStatus(unit).Should().BeFalse();
    }

    [Fact]
    public void StartingStrength_AttachedUnit_CombinesBodyguardAndEveryAttachedUnit()
    {
        // Bodyguard (Crusader Squad, 5 models) + Leader (1) + Support (1) = 7.
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        HalfStrengthResolution.StartingStrength(attachedUnit).Should().Be(7);
    }

    [Fact]
    public void IsAtOrBelowHalfStrength_AttachedUnit_UsesCombinedStrengthRegardlessOfDistribution()
    {
        // Combined starting strength 7, half = floor(7/2) = 3.
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        HalfStrengthResolution.IsAtOrBelowHalfStrength(attachedUnit).Should().BeFalse(); // 7 > 3

        // Kill the Bodyguard's 3-model Initiate line: remaining = (5-3) + 1 (Leader) + 1 (Support) = 4.
        var threeModelLine = attachedUnit.Bodyguard.ModelLines.Single(ml => ml.Count == 3);
        threeModelLine.RemoveCasualties(3);
        HalfStrengthResolution.IsAtOrBelowHalfStrength(attachedUnit).Should().BeFalse(); // 4 > 3

        // One more casualty on the Bodyguard's 2-model Initiate line: remaining = 1 + 0 + 1 + 1 = 3.
        var twoModelLine = attachedUnit.Bodyguard.ModelLines.Single(ml => ml.Count == 2);
        twoModelLine.RemoveCasualties(1);

        HalfStrengthResolution.IsAtOrBelowHalfStrength(attachedUnit).Should().BeTrue(); // 3 <= 3
    }
}
