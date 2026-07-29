using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class AttachedUnitAggregatorTests
{
    [Fact]
    public void StatlineView_TreatsDifferentlyNamedStatlinesAsDistinct_EvenWithEqualValues()
    {
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var view = AttachedUnitAggregator.Build(unit);

        //Two statlines
        view.Statlines.Should().HaveCount(2);
        var troop = view.Statlines.Single(s => s.StatlineName == "Assault Intercessor");
        var sergeant = view.Statlines.Single(s => s.StatlineName == "Assault Intercessor Sergeant");

        //And both statlines are equal
        troop.Statline.Should().Be(sergeant.Statline);
    }

    [Fact]
    public void StatlineView_DropsAStatlineOnceAllItsModelsAreGone()
    {
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var sergeant = unit.ModelLines.Single(x => x.StatlineName == "Assault Intercessor Sergeant");
        sergeant.RemoveCasualties(sergeant.Count);

        var after = AttachedUnitAggregator.Build(unit);

        after.Statlines.Should().HaveCount(1);
        after.Statlines.Should().ContainSingle(s => s.StatlineName == "Assault Intercessor");
    }

    [Fact]
    public void WeaponView_CombinesSameStructuralProfileFromDifferentComponents()
    {
        var attachedUnit = AggregateViewFixtures.WeaponAggregationAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Weapons.Should().ContainSingle();
        view.Weapons[0].Count.Should().Be(5); // 4 Bodyguard + 1 Leader, same structural profile
    }

    [Fact]
    public void WeaponView_CountReflectsCasualties()
    {
        var attachedUnit = AggregateViewFixtures.WeaponAggregationAttachedUnit();
        attachedUnit.Bodyguard.ModelLines[0].RemoveCasualties(2);

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Weapons.Should().ContainSingle();
        view.Weapons[0].Count.Should().Be(3);
    }

    [Fact]
    public void AbilityView_UnitScopedAbilityAppearsInTheCombinedList()
    {
        var attachedUnit = AggregateViewFixtures.ModelScopedAbilityAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.UnitScopedAbilities.Should().ContainSingle(a => a.Name == "Righteous Zeal");
    }

    [Fact]
    public void AbilityView_ModelScopedAbilityStaysWithItsModelLine()
    {
        var attachedUnit = AggregateViewFixtures.ModelScopedAbilityAttachedUnit();
        var leaderLine = attachedUnit.Attached[0].ModelLines[0];

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.ModelScopedAbilities.Should()
            .ContainSingle(e => e.Ability.Name == "Iron Halo" && e.ModelLine == leaderLine);
        view.UnitScopedAbilities.Should().NotContain(a => a.Name == "Iron Halo");
    }

    [Fact]
    public void AbilityView_ModelScopedAbilityDisappearsWhenItsBearerIsRemoved()
    {
        var attachedUnit = AggregateViewFixtures.ModelScopedAbilityAttachedUnit();
        var leaderLine = attachedUnit.Attached[0].ModelLines[0];
        leaderLine.RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.ModelScopedAbilities.Should().BeEmpty();
        view.UnitScopedAbilities.Should().NotContain(a => a.Name == "Iron Halo");
    }

    [Fact]
    public void KeywordView_IsWiredToTheLiveKeywordUnion()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Keywords.Should().BeEquivalentTo(KeywordResolution.EffectiveKeywords(attachedUnit));
    }
}