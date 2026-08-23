using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers ICombatUnit's two new player-set live-state flags (half-strength-and-
/// battleshock-indicators) - default value, independence from each other, and independence from
/// casualty adjustments. Exercised against both a plain Unit and an AttachedUnit, since each
/// implements ICombatUnit separately (an AttachedUnit's flags are its own, not read through to its
/// components - see AttachedUnit's own doc comments).</summary>
public class UnitStatusFlagsTests
{
    [Fact]
    public void Unit_BothFlagsDefaultToFalse()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();

        unit.IsHalfStrengthOverride.Should().BeFalse();
        unit.IsBattleShocked.Should().BeFalse();
    }

    [Fact]
    public void AttachedUnit_BothFlagsDefaultToFalse()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        attachedUnit.IsHalfStrengthOverride.Should().BeFalse();
        attachedUnit.IsBattleShocked.Should().BeFalse();
    }

    [Fact]
    public void SettingHalfStrengthOverride_DoesNotAffectBattleShocked()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();

        unit.IsHalfStrengthOverride = true;

        unit.IsBattleShocked.Should().BeFalse();
    }

    [Fact]
    public void SettingBattleShocked_DoesNotAffectHalfStrengthOverride()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();

        unit.IsBattleShocked = true;

        unit.IsHalfStrengthOverride.Should().BeFalse();
    }

    [Fact]
    public void ClearingBattleShocked_IsAnExplicitPlayerAction_NeverImplicit()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();
        unit.IsBattleShocked = true;

        unit.IsBattleShocked = false;

        unit.IsBattleShocked.Should().BeFalse();
    }

    [Fact]
    public void ACasualtyAdjustment_DoesNotChangeEitherFlag()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();
        unit.IsHalfStrengthOverride = true;
        unit.IsBattleShocked = true;

        foreach (var modelLine in unit.ModelLines)
            modelLine.RemoveCasualties(1);

        unit.IsHalfStrengthOverride.Should().BeTrue();
        unit.IsBattleShocked.Should().BeTrue();
    }

    [Fact]
    public void AnAttachedUnitsFlags_AreItsOwn_NotItsComponents()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        attachedUnit.IsBattleShocked = true;

        // The flag lives on the AttachedUnit itself, not propagated onto its own Bodyguard/Attached
        // Unit instances - each component's own IsBattleShocked is a separate, unrelated property.
        attachedUnit.Bodyguard.IsBattleShocked.Should().BeFalse();
        attachedUnit.Attached.Should().OnlyContain(u => !u.IsBattleShocked);
    }
}
