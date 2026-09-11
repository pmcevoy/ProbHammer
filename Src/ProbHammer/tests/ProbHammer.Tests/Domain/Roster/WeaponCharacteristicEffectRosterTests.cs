using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers weapon-characteristic effect resolution against a small, hand-built
/// <see cref="RuleClassificationBaseline"/> fixture, matched via <see cref="AttachedUnitAggregator.Build"/>
/// directly - the baseline lookup, bearer-scope match, and weapon-selector match all run inside
/// Build, so their effect is only observable through the aggregate view it produces. Mirrors
/// <c>StatlineFlagRuleTests</c>'s own precedent for testing a private mechanism only through its
/// one public entry point.</summary>
public class WeaponCharacteristicEffectRosterTests
{
    [Fact]
    public void BearerScopedWeaponEffect_SplitsAnOtherwiseMergedGroup()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var boost = new Ability
        {
            Name = "Blessed Blade",
            Text = "Improve the Strength characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: boost.Text,
                Target: new SelfRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);

        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Sword Brother", [weapon.Name], count: 4)]);

        var leaderDatasheet = new Datasheet(
            "Marshal", factionKeywords: [], keywords: [], abilities: [boost],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [leader]);

        var view = AttachedUnitAggregator.Build(attachedUnit, baseline);

        view.Weapons.Should().HaveCount(2);
        var unaffected = view.Weapons.Single(w => w.Contributions.Any(c => c.ComponentName == "Sword Brethren Squad"));
        unaffected.Profile.S.Value.Should().Be((CharacteristicValue)4);
        unaffected.Profile.S.ContributingAbilities.Should().BeEmpty();

        var affected = view.Weapons.Single(w => w.Contributions.Any(c => c.ComponentName == "Marshal"));
        affected.Profile.S.Value.Should().Be((CharacteristicValue)5);
        affected.Profile.S.ContributingAbilities.Should().ContainSingle(a => a.Name == "Blessed Blade");
    }

    [Fact]
    public void UnitScopedWeaponEffect_KeepsEveryReachedContributorMerged()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var banner = new Ability
        {
            Name = "War Banner",
            Text = "Improve the Strength characteristic of melee weapons equipped by models in the bearer's unit by 1.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.OptionalGrant
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: banner.Text,
                Target: new AttachedUnitRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);

        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Sword Brother", [weapon.Name], count: 4, abilities: [banner])]);

        var leaderDatasheet = new Datasheet(
            "Marshal", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [leader]);

        var view = AttachedUnitAggregator.Build(attachedUnit, baseline);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(15)); // 4x3 (Bodyguard) + 1x3 (Marshal), still one group
    }

    [Fact]
    public void CaveatedBaselineEntry_DoesNotMutateAWeaponProfile()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var conditional = new Ability
        {
            Name = "Furious Charge",
            Text = "Once per battle, if this model made a Charge move this turn, improve the Strength " +
                   "characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: conditional.Text,
                Target: new SelfRuleTarget(),
                Effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                IsCaveated: true)
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [conditional],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)4);
        entry.Profile.S.ContributingAbilities.Should().BeEmpty();
        entry.UnresolvedAbilities.Should().ContainSingle(a => a.Name == "Furious Charge");
        entry.Contributions.Single().UnresolvedAbilities.Should().ContainSingle(a => a.Name == "Furious Charge");
    }

    [Fact]
    public void ResolvedMutationAndUnresolvedReference_CanCoexistOnOneContribution()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var resolvedBoost = new Ability
        {
            Name = "Blessed Blade",
            Text = "Improve the Strength characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var caveatedBoost = new Ability
        {
            Name = "Furious Charge",
            Text = "Once per battle, if this model made a Charge move this turn, improve the Armour " +
                   "Penetration characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: resolvedBoost.Text,
                Target: new SelfRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ]),
            new RuleClassificationBaselineEntry(
                Text: caveatedBoost.Text,
                Target: new SelfRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "AP", EffectVerb.Improve, 1)
                ],
                IsCaveated: true)
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [resolvedBoost, caveatedBoost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.Profile.S.ContributingAbilities.Should().ContainSingle(a => a.Name == "Blessed Blade");
        entry.Profile.Ap.Value.Should().Be((CharacteristicValue)(-1)); // AP effect stayed unresolved (caveated)
        entry.UnresolvedAbilities.Should().ContainSingle(a => a.Name == "Furious Charge");
    }

    [Fact]
    public void AggregateWeaponEntry_UnresolvedAbilities_AggregatesAcrossContributionsInFirstEncounteredOrder()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var firstCaveat = new Ability
        {
            Name = "Furious Charge",
            Text = "Once per battle, if this model made a Charge move this turn, improve the Strength " +
                   "characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var secondCaveat = new Ability
        {
            Name = "Vengeful Strike",
            Text = "Each time this model's unit ends a Charge move, improve the Strength characteristic " +
                   "of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: firstCaveat.Text,
                Target: new SelfRuleTarget(),
                Effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                IsCaveated: true),
            new RuleClassificationBaselineEntry(
                Text: secondCaveat.Text,
                Target: new SelfRuleTarget(),
                Effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                IsCaveated: true)
        ]);

        var firstDatasheet = new Datasheet(
            "First Squad", factionKeywords: [], keywords: [], abilities: [firstCaveat],
            statlines: [("First Squad", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var firstUnit = new Unit(firstDatasheet, [], [new ModelLine("First Squad", [weapon.Name], count: 1)]);

        var secondDatasheet = new Datasheet(
            "Second Squad", factionKeywords: [], keywords: [], abilities: [secondCaveat],
            statlines: [("Second Squad", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var secondUnit = new Unit(secondDatasheet, [], [new ModelLine("Second Squad", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(firstUnit, [secondUnit]);

        var view = AttachedUnitAggregator.Build(attachedUnit, baseline);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.UnresolvedAbilities.Should().HaveCount(2);
        entry.UnresolvedAbilities.Select(a => a.Name).Should().Equal("Furious Charge", "Vengeful Strike");
    }

    [Fact]
    public void NoCaveatedMatch_ReportsEmptyUnresolvedAbilities()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, RuleClassificationBaseline.FromEntries([]));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.UnresolvedAbilities.Should().BeEmpty();
        entry.Contributions.Single().UnresolvedAbilities.Should().BeEmpty();
    }

    [Fact]
    public void AllWeaponsSelector_MutatesEveryWeaponRegardlessOfType()
    {
        var meleeWeapon = new MeleeWeapon("Chainsword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var rangedWeapon = new RangedWeapon("Bolt pistol", Range: 12, A: 1, Bs: 3, S: 4, Ap: 0, D: 1);
        var boost = new Ability
        {
            Name = "Universal Boost",
            Text = "Improve the Strength characteristic of weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: boost.Text,
                Target: new SelfRuleTarget(),
                Effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [meleeWeapon, rangedWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [meleeWeapon.Name, rangedWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        view.Weapons.Should().HaveCount(2);
        view.Weapons.Should().OnlyContain(w => w.Profile.S.Value == (CharacteristicValue)5);
    }

    [Fact]
    public void ClassQualifiedSelector_LeavesTheNonMatchingWeaponTypeUnaffected()
    {
        var meleeWeapon = new MeleeWeapon("Chainsword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var rangedWeapon = new RangedWeapon("Bolt pistol", Range: 12, A: 1, Bs: 3, S: 4, Ap: 0, D: 1);
        var boost = new Ability
        {
            Name = "Melee Boost",
            Text = "Improve the Strength characteristic of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: boost.Text,
                Target: new SelfRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [meleeWeapon, rangedWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [meleeWeapon.Name, rangedWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var melee = view.Weapons.Single(w => w.Profile.Type == WeaponType.Melee);
        melee.Profile.S.Value.Should().Be((CharacteristicValue)5);
        var ranged = view.Weapons.Single(w => w.Profile.Type == WeaponType.Ranged);
        ranged.Profile.S.Value.Should().Be((CharacteristicValue)4);
    }

    [Fact]
    public void NamedWeaponSelector_MatchesOnlyTheExactlyNamedWeapon()
    {
        var swordWeapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var axeWeapon = new MeleeWeapon("Power axe", A: 3, Ws: 3, S: 6, Ap: -2, D: 1);
        var boost = new Ability
        {
            Name = "Named Boost",
            Text = "Improve the Strength characteristic of this model's Power sword by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: boost.Text,
                Target: new SelfRuleTarget(),
                Effects: [new WeaponCharacteristicEffect(new NamedWeapon("Power sword"), "S", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [swordWeapon, axeWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [swordWeapon.Name, axeWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        view.Weapons.Should().HaveCount(2);
        var sword = view.Weapons.Single(w => w.Contributions.Single().Name == "Power sword");
        sword.Profile.S.Value.Should().Be((CharacteristicValue)5);
        var axe = view.Weapons.Single(w => w.Contributions.Single().Name == "Power axe");
        axe.Profile.S.Value.Should().Be((CharacteristicValue)6); // unaffected, its own original value
    }

    [Fact]
    public void EffectNamingBothAResolvableCharacteristicAndAttacks_LeavesAttacksUnchanged()
    {
        // Zealot-shaped: "improve the Strength and Attacks characteristics... by 1" splits into two
        // atomic WeaponCharacteristicEffects sharing one selector - only the Strength one resolves.
        var weapon = new MeleeWeapon("Chainsword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var zealousFury = new Ability
        {
            Name = "Zealous Fury",
            Text = "Improve the Strength and Attacks characteristics of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: zealousFury.Text,
                Target: new SelfRuleTarget(),
                Effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1),
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [zealousFury],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(3)); // Attacks left unresolved - 1 model x A3
    }
}