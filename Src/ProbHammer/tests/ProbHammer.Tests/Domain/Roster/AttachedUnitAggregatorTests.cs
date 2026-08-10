using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
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
    public void StatlineView_InitialCountSumsAcrossModelLines_EvenWhenOneIsFullyRemoved()
    {
        var unit = UnitFixtures.CrusaderSquadMixedLoadout();
        var powerFistLine = unit.ModelLines.Single(x => x.Weapons.Contains("Power fist"));
        powerFistLine.RemoveCasualties(powerFistLine.Count);

        var view = AttachedUnitAggregator.Build(unit);

        var initiate = view.Statlines.Single(s => s.StatlineName == "Initiate");
        initiate.InitialCount.Should().Be(5); // 2 (removed) + 3 (surviving)
        initiate.RemainingCount.Should().Be(3);
    }

    [Fact]
    public void StatlineView_LoadoutBreakdownIncludesFullyRemovedModelLine()
    {
        var unit = UnitFixtures.CrusaderSquadMixedLoadout();
        var powerFistLine = unit.ModelLines.Single(x => x.Weapons.Contains("Power fist"));
        powerFistLine.RemoveCasualties(powerFistLine.Count);

        var view = AttachedUnitAggregator.Build(unit);

        var initiate = view.Statlines.Single(s => s.StatlineName == "Initiate");
        initiate.Loadouts.Should().HaveCount(2);

        var removed = initiate.Loadouts.Single(l => l.WeaponsLabel.Contains("Power fist"));
        removed.RemainingCount.Should().Be(0);
        removed.InitialCount.Should().Be(2);

        var surviving = initiate.Loadouts.Single(l => l.WeaponsLabel.Contains("Master-crafted Power Weapon"));
        surviving.RemainingCount.Should().Be(3);
        surviving.InitialCount.Should().Be(3);
    }

    [Fact]
    public void StatlineView_DisappearsEntirelyOnceEveryModelLineSharingItIsGone()
    {
        var unit = UnitFixtures.CrusaderSquadMixedLoadout();
        foreach (var modelLine in unit.ModelLines)
            modelLine.RemoveCasualties(modelLine.Count);

        var view = AttachedUnitAggregator.Build(unit);

        view.Statlines.Should().BeEmpty();
    }

    [Fact]
    public void WeaponView_CombinesSameStructuralProfileFromDifferentComponents_IntoATrueTotal()
    {
        var attachedUnit = AggregateViewFixtures.WeaponAggregationAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Weapons.Should().ContainSingle();
        // 4 Bodyguard models x A3 + 1 Leader model x A7 = 19, not a count-only merge that discards
        // one contributor's Attacks.
        view.Weapons[0].TotalAttacks.Should().Be(DiceExpression.Fixed(19));
    }

    [Fact]
    public void WeaponView_TotalAttacksReflectsCasualties()
    {
        var attachedUnit = AggregateViewFixtures.WeaponAggregationAttachedUnit();
        attachedUnit.Bodyguard.ModelLines[0].RemoveCasualties(2);

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Weapons.Should().ContainSingle();
        // 2 surviving Bodyguard models x A3 + 1 Leader model x A7 = 13
        view.Weapons[0].TotalAttacks.Should().Be(DiceExpression.Fixed(13));
    }

    [Fact]
    public void WeaponView_ContributionsListsEachModelLineWithCountAndPerModelAttacks()
    {
        var attachedUnit = AggregateViewFixtures.WeaponAggregationAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        var entry = view.Weapons.Single();
        entry.Contributions.Should().HaveCount(2);

        var bodyguardContribution = entry.Contributions.Single(c => c.StatlineName == "Sword Brother");
        bodyguardContribution.ComponentName.Should().Be("Sword Brethren Squad");
        bodyguardContribution.Count.Should().Be(4);
        bodyguardContribution.PerModelAttacks.Should().Be(DiceExpression.Fixed(3));

        var leaderContribution = entry.Contributions.Single(c => c.StatlineName == "Marshal");
        leaderContribution.ComponentName.Should().Be("Marshal");
        leaderContribution.Count.Should().Be(1);
        leaderContribution.PerModelAttacks.Should().Be(DiceExpression.Fixed(7));
    }

    [Fact]
    public void WeaponView_DifferentlyModifiedSameNamedWeapons_StaySeparate()
    {
        var attachedUnit = AggregateViewFixtures.DifferentlyModifiedWeaponsAttachedUnit();

        var view = AttachedUnitAggregator.Build(attachedUnit);

        view.Weapons.Should().HaveCount(2);
        var lethalHitsCopy = view.Weapons.Single(w => w.Profile.LethalHits);
        var plainCopy = view.Weapons.Single(w => !w.Profile.LethalHits);

        lethalHitsCopy.TotalAttacks.Should().Be(DiceExpression.Fixed(12)); // 4 x A3
        plainCopy.TotalAttacks.Should().Be(DiceExpression.Fixed(7));      // 1 x A7
    }

    [Fact]
    public void WeaponView_WeaponsDifferingOnlyByPistolAssaultOrIgnoresCover_StaySeparate()
    {
        // Regression test: WeaponProfileEqualityKey previously omitted Pistol/Assault/IgnoresCover,
        // so weapons identical on Type/Skill/S/Ap/D but carrying different keyword flags (e.g. a
        // Bolt pistol and an Astartes shotgun) silently merged into one row with a meaningless
        // summed total.
        var datasheet = new Datasheet(
            name: "Scout Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Scout"] = new(6, 4, 4, 2, 6, 1) },
            weaponProfiles:
            [
                new RangedWeapon("Bolt pistol", 12, 1, 3, 4, 0, 1) { Pistol = true },
                new RangedWeapon("Astartes shotgun", 18, 2, 3, 4, 0, 1) { Assault = true },
                new RangedWeapon("Suppressive carbine", 18, 1, 3, 4, 0, 1) { IgnoresCover = true }
            ]);
        var unit = new Unit(
            datasheet, [],
            [new ModelLine("Scout", ["Bolt pistol", "Astartes shotgun", "Suppressive carbine"], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit);

        view.Weapons.Should().HaveCount(3);
        view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Bolt pistol" && w.TotalAttacks == DiceExpression.Fixed(1));
        view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Astartes shotgun" && w.TotalAttacks == DiceExpression.Fixed(2));
        view.Weapons.Should().ContainSingle(w => w.Profile.Name == "Suppressive carbine" && w.TotalAttacks == DiceExpression.Fixed(1));
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
