using ProbHammer.Tests.Domain.Fixtures;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers weapon-characteristic effect resolution against a small, hand-built
/// <see cref="AbilityClassificationCatalogue"/> fixture, matched via <see cref="AttachedUnitAggregator.Build"/>
/// directly - the catalogue lookup, bearer-scope match, and weapon-selector match all run inside
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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);

        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Sword Brother", [weapon.Name], count: 4)]);

        var leaderDatasheet = new Datasheet(
            "Marshal", keywords: [], abilities: [boost],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [leader]);

        var view = AttachedUnitAggregator.Build(attachedUnit, classifications);

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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: banner.Text,
                target: new AttachedUnitRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);

        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Sword Brother", [weapon.Name], count: 4, abilities: [banner])]);

        var leaderDatasheet = new Datasheet(
            "Marshal", keywords: [], abilities: [],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [leader]);

        var view = AttachedUnitAggregator.Build(attachedUnit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(15)); // 4x3 (Bodyguard) + 1x3 (Marshal), still one group
    }

    [Fact]
    public void AConditionalEffect_DoesNotMutateAWeaponProfile()
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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: conditional.Text,
                target: new SelfRuleTarget(),
                effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                conditional: true)
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [conditional],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)4);
        entry.Profile.S.ContributingAbilities.Should().BeEmpty();
        var notApplied = entry.NotAppliedEffects.Should().ContainSingle().Subject;
        notApplied.SourceAbility.Name.Should().Be("Furious Charge");
        notApplied.Characteristic.Should().Be("S");
        notApplied.Amount.Should().Be(1);
        notApplied.Condition.ConditionText.Should().Be("Test condition");
        entry.Contributions.Single().NotAppliedEffects.Should().ContainSingle(e => e.SourceAbility.Name == "Furious Charge");
    }

    [Fact]
    public void OneClassification_AppliesItsUnconditionalEffect_AndDefersItsConditionalOne()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var mixed = new Ability
        {
            Name = "Righteous Fury",
            Text = "Improve the Strength characteristic of melee weapons equipped by this model by 1. " +
                   "If this model made a Charge move this turn, add 1 to their Damage characteristic.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var classification = new AbilityClassification
        {
            Target = new SelfRuleTarget(),
            Effects =
            [
                new ClassifiedEffect
                {
                    Effect = new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve,
                        1),
                    ResidualConditionBucket = ResidualConditionBucket.None
                },
                new ClassifiedEffect
                {
                    Effect = new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "D", EffectVerb.Improve,
                        1),
                    ResidualConditionBucket = ResidualConditionBucket.Never,
                    ConditionText = "This model made a Charge move this turn"
                }
            ],
            CoverageStatus = CoverageStatus.Complete
        };
        var classifications = ClassificationFixtures.Catalogue([(mixed.Text, classification)]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [mixed],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.Profile.D.Value.Should().Be((CharacteristicValue)1);
        entry.NotAppliedEffects.Should().ContainSingle(e =>
            e.SourceAbility.Name == "Righteous Fury" && e.Characteristic == "D" && e.Amount == 1 &&
            e.Condition.ConditionText == "This model made a Charge move this turn");
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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: resolvedBoost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ]),
            ClassificationFixtures.Entry(
                text: caveatedBoost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "AP", EffectVerb.Improve, 1)
                ],
                conditional: true)
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [resolvedBoost, caveatedBoost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.Profile.S.ContributingAbilities.Should().ContainSingle(a => a.Name == "Blessed Blade");
        entry.Profile.Ap.Value.Should().Be((CharacteristicValue)(-1));
        entry.NotAppliedEffects.Should().ContainSingle(e =>
            e.SourceAbility.Name == "Furious Charge" && e.Characteristic == "AP" && e.Amount == -1);
    }

    [Fact]
    public void AggregateWeaponEntry_NotAppliedEffects_AggregatesAcrossContributionsInFirstEncounteredOrder()
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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: firstCaveat.Text,
                target: new SelfRuleTarget(),
                effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                conditional: true),
            ClassificationFixtures.Entry(
                text: secondCaveat.Text,
                target: new SelfRuleTarget(),
                effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)],
                conditional: true)
        ]);

        var firstDatasheet = new Datasheet(
            "First Squad", keywords: [], abilities: [firstCaveat],
            statlines: [("First Squad", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var firstUnit = new Unit(firstDatasheet, [], [new ModelLine("First Squad", [weapon.Name], count: 1)]);

        var secondDatasheet = new Datasheet(
            "Second Squad", keywords: [], abilities: [secondCaveat],
            statlines: [("Second Squad", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var secondUnit = new Unit(secondDatasheet, [], [new ModelLine("Second Squad", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(firstUnit, [secondUnit]);

        var view = AttachedUnitAggregator.Build(attachedUnit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.NotAppliedEffects.Select(e => e.SourceAbility.Name).Should().Equal("Furious Charge", "Vengeful Strike");
    }

    [Fact]
    public void NoConditionalMatch_ReportsNoNotAppliedEffects()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.Catalogue([]));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.NotAppliedEffects.Should().BeEmpty();
        entry.Contributions.Single().NotAppliedEffects.Should().BeEmpty();
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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects: [new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [meleeWeapon, rangedWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [meleeWeapon.Name, rangedWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [meleeWeapon, rangedWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [meleeWeapon.Name, rangedWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

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
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects: [new WeaponCharacteristicEffect(new NamedWeapon("Power sword"), "S", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [swordWeapon, axeWeapon]);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [swordWeapon.Name, axeWeapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        view.Weapons.Should().HaveCount(2);
        var sword = view.Weapons.Single(w => w.Contributions.Single().Name == "Power sword");
        sword.Profile.S.Value.Should().Be((CharacteristicValue)5);
        var axe = view.Weapons.Single(w => w.Contributions.Single().Name == "Power axe");
        axe.Profile.S.Value.Should().Be((CharacteristicValue)6); // unaffected, its own original value
    }

    [Fact]
    public void EffectNamingBothAResolvableCharacteristicAndAttacks_ResolvesBothIndependently()
    {
        // Zealot-shaped: "improve the Strength and Attacks characteristics... by 1" splits into two
        // atomic WeaponCharacteristicEffects sharing one selector - both now resolve independently
        // (resolve-weapon-attacks-effects): Strength mutates the profile, Attacks is recorded as a
        // separate per-contribution amount, neither displacing the other.
        var weapon = new MeleeWeapon("Chainsword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var zealousFury = new Ability
        {
            Name = "Zealous Fury",
            Text = "Improve the Strength and Attacks characteristics of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: zealousFury.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1),
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [zealousFury],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(4)); // 1 model x (base A3 + 1)
        var contribution = entry.Contributions.Single();
        contribution.PerModelAttacks.Should().Be(DiceExpression.Fixed(3)); // base value, unmutated
        contribution.AttacksContributions.Should().ContainSingle(c =>
            c.SourceAbility.Name == "Zealous Fury" && c.Amount == 1);
    }

    [Fact]
    public void MatchedAttacksEffect_RecordsAContributionWithoutChangingPerModelAttacks()
    {
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var boost = new Ability
        {
            Name = "Extra Limbs",
            Text = "Add 1 to the Attacks characteristic of melee weapons equipped by this model.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [boost],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 2)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        var contribution = entry.Contributions.Single();
        contribution.PerModelAttacks.Should().Be(DiceExpression.Fixed(3));
        contribution.AttacksContributions.Should().ContainSingle(c =>
            c.SourceAbility.Name == "Extra Limbs" && c.Amount == 1);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(8)); // 2 models x (3 base + 1)
    }

    [Fact]
    public void RecordedAttacksContribution_DoesNotSplitOrMergeAnOtherwiseIdenticalGroup()
    {
        // Regression against "Same weapon profile from different components is combined": one
        // contribution carries a recorded Attacks amount, the other doesn't - both still merge into
        // one entry, since Attacks plays no part in structural profile equality.
        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var boost = new Ability
        {
            Name = "Extra Limbs",
            Text = "Add 1 to the Attacks characteristic of melee weapons equipped by this model.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: boost.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 1)
                ])
        ]);

        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Sword Brother", [weapon.Name], count: 4)]);

        var leaderDatasheet = new Datasheet(
            "Marshal", keywords: [], abilities: [boost],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);

        var attachedUnit = new AttachedUnit(bodyguard, [leader]);

        var view = AttachedUnitAggregator.Build(attachedUnit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Contributions.Should().HaveCount(2);
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(16)); // 4x3 (Bodyguard) + 1x(3+1) (Marshal)
    }

    [Fact]
    public void EffectNamingAnUnresolvableCharacteristicAlongsideAttacks_LeavesTheUnresolvableOneUnapplied()
    {
        // A hypothetical effect naming a characteristic this mechanism doesn't yet resolve (e.g.
        // Weapon Skill) alongside Attacks - the unresolvable one is silently skipped (the existing
        // GetWeaponScalarField/ApplyWeaponCharacteristicEffect throw path is never reached for it,
        // since it's filtered out by ResolveContributionProfile's own S/AP/D allowlist), while the
        // co-occurring Attacks effect still resolves.
        var weapon = new MeleeWeapon("Chainsword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);
        var hybrid = new Ability
        {
            Name = "Hybrid Boost",
            Text = "Improve the Weapon Skill and Attacks characteristics of melee weapons equipped by this model by 1.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: hybrid.Text,
                target: new SelfRuleTarget(),
                effects:
                [
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "WS", EffectVerb.Improve, 1),
                    new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "A", EffectVerb.Improve, 1)
                ])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [hybrid],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]);
        var unit = new Unit(datasheet, [], [new ModelLine("Some Unit", [weapon.Name], count: 1)]);

        var act = () => AttachedUnitAggregator.Build(unit, classifications);

        act.Should().NotThrow();
        var view = AttachedUnitAggregator.Build(unit, classifications);
        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.TotalAttacks.Should().Be(DiceExpression.Fixed(4)); // 1 model x (base A3 + 1), WS ignored
    }
}