using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers rule-effect flagging against a small, hand-built
/// <see cref="AbilityClassificationCatalogue"/> fixture reproducing Shield Dome's and Vexilla's
/// classifications (<see cref="ClassificationFixtures.ShieldDomeAndVexilla"/>), matched
/// via <see cref="AttachedUnitAggregator.Build"/> directly - the catalogue lookup runs inside Build,
/// so its effect is only observable through the aggregate view it produces.</summary>
public class StatlineFlagRuleTests
{
    private static readonly Ability ShieldDome = new()
    {
        Name = "Shield Dome",
        Text = "The bearer has a 5+ invulnerable save.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    private static readonly Ability Vexilla = new()
    {
        Name = "Vexilla",
        Text = "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    private static Unit ImpulsorWithShieldDome()
    {
        var datasheet = new Datasheet(
            "Impulsor", keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        return new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [ShieldDome])]);
    }

    [Fact]
    public void ShieldDome_FlagsTheBearersOwnInvulnerableSave()
    {
        var unit = ImpulsorWithShieldDome();

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.ShieldDomeAndVexilla);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.DerivedValue!.MeleeInSv.Should().Be(5);
        entry.Statline.InSv.DerivedValue.RangedInSv.Should().Be(5);
        entry.Statline.InSv.ContributingAbilities.Should().ContainSingle(a => a.Name == "Shield Dome");
        // the Datasheet had no base InSv at all - Shield Dome's own mutation must not overwrite that fact
        entry.Statline.InSv.OriginalValue.Should().Be(InvulnerableSave.None);
    }

    [Fact]
    public void ShieldDome_SourceAbilityStillRendersInTheNormalAbilityListing()
    {
        var unit = ImpulsorWithShieldDome();

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.ShieldDomeAndVexilla);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Shield Dome");
    }

    [Fact]
    public void ShieldDome_FlagDisappearsOnceItsBearerIsRemoved()
    {
        var unit = ImpulsorWithShieldDome();
        unit.ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.ShieldDomeAndVexilla);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.DerivedValue!.MeleeInSv.Should().Be(0);
        entry.Statline.InSv.DerivedValue.RangedInSv.Should().Be(0);
        entry.Statline.InSv.ContributingAbilities.Should().BeEmpty();
    }

    [Fact]
    public void UnmatchedAbility_NameMatchesButTextDoesNot_ProducesNoFlag()
    {
        var mismatched = ShieldDome with { Text = "The bearer has a 4+ invulnerable save." };
        var datasheet = new Datasheet(
            "Impulsor", keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [mismatched])]);

        var view = AttachedUnitAggregator.Build(unit, ClassificationFixtures.ShieldDomeAndVexilla);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.OriginalValue.Should().Be(InvulnerableSave.None);
        entry.Statline.InSv.ContributingAbilities.Should().BeEmpty();
    }

    private static AttachedUnit CustodianGuardWithVexilla()
    {
        var bodyguardDatasheet = new Datasheet(
            "Custodian Guard", keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Custodian Guard", [], count: 4, abilities: [Vexilla])]);

        var wardenDatasheet = new Datasheet(
            "Custodian Warden", keywords: [], abilities: [],
            statlines: [("Custodian Warden", new Statline(6, 6, 2, 5, 7, 2))], weaponProfiles: []);
        var warden = new Unit(wardenDatasheet, [], [new ModelLine("Custodian Warden", [], count: 1)]);

        return new AttachedUnit(bodyguard, [warden]);
    }

    [Fact]
    public void Vexilla_FlagsObjectiveControlOnEveryRowOfTheWholeUnit_NotOnlyTheBearersOwnRow()
    {
        var attachedUnit = CustodianGuardWithVexilla();

        var view = AttachedUnitAggregator.Build(attachedUnit, ClassificationFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().HaveCount(2);
        var bodyguardEntry = view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard").Subject;
        bodyguardEntry.Statline.Oc.Value.Should().Be((CharacteristicValue)3);
        // the catalogue's own base OC (2) must survive Vexilla's mutation, not be overwritten by the +1 result
        bodyguardEntry.Statline.Oc.OriginalValue.Should().Be((CharacteristicValue)2);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Warden" && s.Statline.Oc.Value == 3);
    }

    private static (string Text, AbilityClassification Classification) VexillaEntry(bool conditional = false) =>
        ClassificationFixtures.Entry(
            text: Vexilla.Text,
            target: new AttachedUnitRuleTarget(),
            effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)],
            conditional: conditional);

    [Fact]
    public void AConditionalEffect_ProducesNoFlaggedValue_AndTheAbilityStillRenders()
    {
        var attachedUnit = CustodianGuardWithVexilla();

        var view = AttachedUnitAggregator.Build(attachedUnit,
            ClassificationFixtures.Catalogue([VexillaEntry(conditional: true)]));

        view.Statlines.Should()
            .OnlyContain(s => s.Statline.Oc.Value == 2 && s.Statline.Oc.ContributingAbilities.Count == 0);
        view.Abilities.Should().Contain(e => e.Ability.Name == "Vexilla");
    }

    [Fact]
    public void AUsageLimitedEffect_ProducesNoFlaggedValue()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        var (text, classification) = VexillaEntry();

        var view = AttachedUnitAggregator.Build(attachedUnit,
            ClassificationFixtures.Catalogue([(text, classification with { UsageLimit = UsageLimit.OncePerBattle })]));

        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.Value == 2);
    }

    [Fact]
    public void APartialRecordsUnconditionalEffect_StillApplies()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        var (text, classification) = VexillaEntry();
        var partial = classification with
        {
            CoverageStatus = CoverageStatus.Partial,
            UnclassifiedResidue = "Something this schema can't represent"
        };

        var view = AttachedUnitAggregator.Build(attachedUnit, ClassificationFixtures.Catalogue([(text, partial)]));

        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.Value == 3);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ACaveatedInvulnerableSave_ResolvesOnlyFromAnUnconditionalEffect(bool conditional, bool staysCaveated)
    {
        var caveat = new Ability
        {
            Name = "Invulnerable Save (4+*)",
            Text = "This model has a 4+ invulnerable save against melee attacks.",
            Scope = AbilityScope.Model,
            Origin = AbilityOrigin.Intrinsic
        };
        var datasheet = new Datasheet(
            "Judiciar", keywords: [], abilities: [],
            statlines:
            [
                ("Judiciar", new Statline(6, 4, 3, 4, 6, 1)
                    { InSv = InvulnerableSaveCharacteristicView.Caveated(4, 4, caveat) })
            ],
            weaponProfiles: []);
        var unit = new Unit(datasheet, [], [new ModelLine("Judiciar", [], count: 1)]);
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: caveat.Text,
                target: new SelfRuleTarget(),
                effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(4, 0))],
                conditional: conditional)
        ]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        view.Statlines.Should().ContainSingle().Which.Statline.InSv.IsCaveated.Should().Be(staysCaveated);
    }

    [Fact]
    public void Vexilla_UnrelatedCasualtyElsewhereInTheUnit_LeavesTheFlagUntouched()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Attached[0].ModelLines[0].RemoveCasualties(1); // the Warden, not the Vexilla bearer

        var view = AttachedUnitAggregator.Build(attachedUnit, ClassificationFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard" && s.Statline.Oc.Value == 3);
    }

    [Fact]
    public void Vexilla_FlagDisappearsOnceItsBearerModelLineIsFullyRemoved()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Bodyguard.ModelLines[0].RemoveCasualties(4);

        var view = AttachedUnitAggregator.Build(attachedUnit, ClassificationFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.Value == 2);
    }

    [Fact]
    public void KeywordScopedMatch_ProducesNoFlaggedValue()
    {
        var keywordScoped = new Ability
        {
            Name = "Faith-Fuelled Resolve",
            Text = "Friendly SWORD BRETHREN SQUAD units have +1 OC.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.OptionalGrant
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: keywordScoped.Text,
                target: new KeywordRuleTarget(["SWORD BRETHREN SQUAD"]),
                effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Sword Brother", [], count: 1, abilities: [keywordScoped])]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.Oc.IsCaveated.Should().BeFalse();
        entry.Statline.Oc.ContributingAbilities.Should().BeEmpty();
        entry.Statline.Oc.Value.Should().Be((CharacteristicValue)1);
    }

    [Fact]
    public void DetachmentRuleOriginAbility_AppliesAsWholeUnitScoped_DespiteItsOwnKeywordClassifiedTarget()
    {
        var inboundAbility = new Ability
        {
            Name = "Faith-Fuelled Resolve",
            Text = "Friendly SWORD BRETHREN SQUAD units have +1 OC.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.DetachmentRule
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: inboundAbility.Text,
                target: new KeywordRuleTarget(["SWORD BRETHREN SQUAD"]),
                effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
        ]);
        var bodyguardDatasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 2))], weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Sword Brother", [], count: 4)]);
        var leaderDatasheet = new Datasheet(
            "Ancient", keywords: [], abilities: [],
            statlines: [("Ancient", new Statline(6, 5, 3, 4, 6, 1))], weaponProfiles: []);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Ancient", [], count: 1)]);
        var attachedUnit = new AttachedUnit(bodyguard, [leader]) { InboundAbilities = [inboundAbility] };

        var view = AttachedUnitAggregator.Build(attachedUnit, classifications);

        view.Statlines.Should().HaveCount(2);
        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.ContributingAbilities.Count == 1);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Sword Brother" && s.Statline.Oc.Value == 3);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Ancient" && s.Statline.Oc.Value == 2);
    }

    private static readonly Ability ArmyRule = new()
    {
        Name = "Waaagh!", Text = "Army Rule test text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.ArmyRule
    };

    private static AttachedUnit BoyzLedByWarboss()
    {
        var bodyguardDatasheet = new Datasheet(
            "Boyz", keywords: [], abilities: [ArmyRule],
            statlines: [("Boy", new Statline(6, 5, 5, 1, 7, 2))], weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Boy", [], count: 9)]);
        var leaderDatasheet = new Datasheet(
            "Warboss", keywords: [], abilities: [],
            statlines: [("Warboss", new Statline(6, 5, 4, 6, 6, 1))], weaponProfiles: []);
        var leader = new Unit(leaderDatasheet, [], [new ModelLine("Warboss", [], count: 1)]);
        return new AttachedUnit(bodyguard, [leader]);
    }

    [Theory]
    [MemberData(nameof(ArmyRuleTargets))]
    public void ArmyRuleOriginAbility_AppliesToEveryRowOfTheWholeUnit_WhateverItsClassifiedTarget(RuleTarget target)
    {
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: ArmyRule.Text, target: target,
                effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
        ]);

        var view = AttachedUnitAggregator.Build(BoyzLedByWarboss(), classifications);

        view.Statlines.Should().HaveCount(2);
        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.ContributingAbilities.Count == 1);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Boy" && s.Statline.Oc.Value == 3);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Warboss" && s.Statline.Oc.Value == 2);
    }

    public static TheoryData<RuleTarget> ArmyRuleTargets() =>
        new() { new KeywordRuleTarget(["ORKS"]), new SelfRuleTarget() };

    [Fact]
    public void ArmyRuleOriginAbility_AConditionalInvulnerableSave_IsRecordedOnEveryRow_NotApplied()
    {
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: ArmyRule.Text, target: new KeywordRuleTarget(["ORKS"]),
                effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5))], conditional: true)
        ]);

        var view = AttachedUnitAggregator.Build(BoyzLedByWarboss(), classifications);

        view.Statlines.Should().HaveCount(2).And.OnlyContain(s =>
            s.Statline.InSv.ContributingAbilities.Count == 0 &&
            s.NotAppliedEffects.Count == 1 &&
            s.NotAppliedEffects[0].SourceAbility.Name == "Waaagh!" &&
            s.NotAppliedEffects[0].Effect is InvulnerableSaveCharacteristicEffect);
    }

    [Fact]
    public void UnconditionallyRosterWideMatch_ProducesNoFlaggedValue()
    {
        var unconditional = new Ability
        {
            Name = "Some Army-Wide Rule",
            Text = "Add 1 to the Toughness characteristic.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.OptionalGrant
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: unconditional.Text,
                target: new UnconditionalRuleTarget(),
                effects: [new ScalarCharacteristicEffect("T", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", keywords: [], abilities: [],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [], count: 1, abilities: [unconditional])]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.T.IsCaveated.Should().BeFalse();
        entry.Statline.T.ContributingAbilities.Should().BeEmpty();
        entry.Statline.T.Value.Should().Be((CharacteristicValue)4);
    }

    // Same-field stacking between two matched classifications - the first applied wins, the second
    // is skipped.
    [Fact]
    public void TwoMatchesTargetingTheSameCharacteristic_TheFirstAppliedWins_TheSecondIsSkipped()
    {
        var abilityA = new Ability
        {
            Name = "First Grant", Text = "Add 1 to the Wounds characteristic.",
            Scope = AbilityScope.Model, Origin = AbilityOrigin.Enhancement
        };
        var abilityB = new Ability
        {
            Name = "Second Grant", Text = "Add 2 to the Wounds characteristic.",
            Scope = AbilityScope.Model, Origin = AbilityOrigin.Enhancement
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: abilityA.Text, target: new SelfRuleTarget(),
                effects: [new ScalarCharacteristicEffect("W", EffectVerb.Improve, 1)]),
            ClassificationFixtures.Entry(
                text: abilityB.Text, target: new SelfRuleTarget(),
                effects: [new ScalarCharacteristicEffect("W", EffectVerb.Improve, 2)])
        ]);
        var datasheet = new Datasheet(
            "Custodian Guard", keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Custodian Guard", [], count: 1, abilities: [abilityA, abilityB])]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.W.IsCaveated.Should().BeFalse();
        entry.Statline.W.Value.Should().Be((CharacteristicValue)5); // base 4 + First Grant's 1, not + Second's 2
        entry.Statline.W.ContributingAbilities.Should().ContainSingle(a => a.Name == "First Grant");
    }

    private static readonly Ability MartialHonour = new()
    {
        Name = "Martial Honour", Text = "Martial Honour test text.",
        Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic
    };

    private static readonly AbilityClassificationCatalogue MartialHonourCatalogue = ClassificationFixtures.Catalogue(
    [
        ClassificationFixtures.Entry(
            text: MartialHonour.Text, target: new SelfRuleTarget(),
            effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 5)], conditional: true)
    ]);

    private static Unit CrusaderSquadWithMartialHonour()
    {
        var datasheet = new Datasheet(
            "Crusader Squad", keywords: [], abilities: [],
            statlines:
            [
                ("Sword Brother", new Statline(6, 4, 3, 2, 6, 2)),
                ("Initiate", new Statline(6, 4, 3, 2, 7, 2))
            ],
            weaponProfiles: []);
        return new Unit(datasheet, [],
        [
            new ModelLine("Sword Brother", [], count: 1, abilities: [MartialHonour]),
            new ModelLine("Initiate", [], count: 4)
        ]);
    }

    [Fact]
    public void AConditionalObjectiveControlEffect_IsRecordedOnTheBearer_NotApplied()
    {
        var view = AttachedUnitAggregator.Build(CrusaderSquadWithMartialHonour(), MartialHonourCatalogue);

        var bearer = view.Statlines.Should().ContainSingle(s => s.StatlineName == "Sword Brother").Subject;
        bearer.Statline.Oc.Value.Should().Be((CharacteristicValue)2);
        var notApplied = bearer.NotAppliedEffects.Should().ContainSingle().Subject;
        notApplied.SourceAbility.Name.Should().Be("Martial Honour");
        notApplied.Effect.Should().Be(new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 5));
        notApplied.Condition.ConditionText.Should().Be("Test condition");
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Initiate")
            .Which.NotAppliedEffects.Should().BeEmpty();
    }

    [Fact]
    public void AConditionalInvulnerableSave_IsRecordedOnAUnitWithoutOne_AndTheSaveStaysAbsent()
    {
        var waaagh = new Ability
        {
            Name = "Waaagh!", Text = "Waaagh! test text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: waaagh.Text, target: new AttachedUnitRuleTarget(),
                effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5))], conditional: true)
        ]);
        var datasheet = new Datasheet(
            "Boyz", keywords: [], abilities: [waaagh],
            statlines: [("Boy", new Statline(6, 5, 5, 1, 7, 2)), ("Boss Nob", new Statline(6, 5, 5, 2, 7, 2))],
            weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Boy", [], count: 9), new ModelLine("Boss Nob", [], count: 1)]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        view.Statlines.Should().HaveCount(2).And.OnlyContain(s =>
            s.Statline.InSv.OriginalValue == InvulnerableSave.None &&
            s.Statline.InSv.ContributingAbilities.Count == 0 &&
            s.NotAppliedEffects.Count == 1 &&
            s.NotAppliedEffects[0].SourceAbility.Name == "Waaagh!" &&
            s.NotAppliedEffects[0].Effect is InvulnerableSaveCharacteristicEffect);
    }

    [Fact]
    public void AConditionalEffect_IsNoLongerRecordedOnceItsBearerIsRemoved()
    {
        var unit = CrusaderSquadWithMartialHonour();
        unit.ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit, MartialHonourCatalogue);

        view.Statlines.Should().OnlyContain(s => s.NotAppliedEffects.Count == 0);
    }

    [Fact]
    public void AnActivatedObjectiveControlEffect_IsApplied_AndNotRecorded()
    {
        var unit = CrusaderSquadWithMartialHonour();
        unit.ConditionActivations = ActivationFixtures.Condition("Martial Honour", "Test condition");

        var view = AttachedUnitAggregator.Build(unit, MartialHonourCatalogue);

        var bearer = view.Statlines.Single(s => s.StatlineName == "Sword Brother");
        bearer.Statline.Oc.Value.Should().Be((CharacteristicValue)7);
        bearer.Statline.Oc.ContributingAbilities.Should().ContainSingle(a => a.Name == "Martial Honour");
        view.Statlines.Should().OnlyContain(s => s.NotAppliedEffects.Count == 0);
        view.Statlines.Single(s => s.StatlineName == "Initiate").Statline.Oc.Value
            .Should().Be((CharacteristicValue)2);
    }

    [Fact]
    public void AnActivatedInvulnerableSave_IsApplied_AndNotRecorded()
    {
        var waaagh = new Ability
        {
            Name = "Waaagh!", Text = "Waaagh! test text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: waaagh.Text, target: new AttachedUnitRuleTarget(),
                effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5))], conditional: true)
        ]);
        var datasheet = new Datasheet(
            "Boyz", keywords: [], abilities: [waaagh],
            statlines: [("Boy", new Statline(6, 5, 5, 1, 7, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [], [new ModelLine("Boy", [], count: 10)])
        {
            ConditionActivations = ActivationFixtures.Condition("WAAAGH!", "Test condition")
        };

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.DerivedValue!.MeleeInSv.Should().Be(5);
        entry.Statline.InSv.ContributingAbilities.Should().ContainSingle(a => a.Name == "Waaagh!");
        entry.NotAppliedEffects.Should().BeEmpty();
    }

    [Fact]
    public void AKeywordTargetedConditionalEffect_IsNotRecorded()
    {
        var keywordScoped = new Ability
        {
            Name = "Keyword Grant", Text = "Keyword-targeted conditional test text.",
            Scope = AbilityScope.Unit, Origin = AbilityOrigin.OptionalGrant
        };
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: keywordScoped.Text, target: new KeywordRuleTarget(["SWORD BRETHREN SQUAD"]),
                effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)], conditional: true)
        ]);
        var datasheet = new Datasheet(
            "Sword Brethren Squad", keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Sword Brother", [], count: 1, abilities: [keywordScoped])]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        view.Statlines.Should().ContainSingle().Which.NotAppliedEffects.Should().BeEmpty();
    }
}