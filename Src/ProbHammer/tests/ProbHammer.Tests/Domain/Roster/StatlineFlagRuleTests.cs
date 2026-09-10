using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers rule-effect flagging against a small, hand-built
/// <see cref="RuleClassificationBaseline"/> fixture reproducing Shield Dome's and Vexilla's real
/// baseline entries (<see cref="RuleClassificationBaselineFixtures.ShieldDomeAndVexilla"/>), matched
/// via <see cref="AttachedUnitAggregator.Build"/> directly - the baseline lookup runs inside Build,
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
            "Impulsor", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        return new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [ShieldDome])]);
    }

    [Fact]
    public void ShieldDome_FlagsTheBearersOwnInvulnerableSave()
    {
        var unit = ImpulsorWithShieldDome();

        var view = AttachedUnitAggregator.Build(unit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

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

        var view = AttachedUnitAggregator.Build(unit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

        view.Abilities.Should().ContainSingle(e => e.Ability.Name == "Shield Dome");
    }

    [Fact]
    public void ShieldDome_FlagDisappearsOnceItsBearerIsRemoved()
    {
        var unit = ImpulsorWithShieldDome();
        unit.ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

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
            "Impulsor", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Impulsor", new Statline(12, 9, 3, 11, 6, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [], [new ModelLine("Impulsor", [], count: 1, abilities: [mismatched])]);

        var view = AttachedUnitAggregator.Build(unit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.OriginalValue.Should().Be(InvulnerableSave.None);
        entry.Statline.InSv.ContributingAbilities.Should().BeEmpty();
    }

    private static AttachedUnit CustodianGuardWithVexilla()
    {
        var bodyguardDatasheet = new Datasheet(
            "Custodian Guard", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [],
            [new ModelLine("Custodian Guard", [], count: 4, abilities: [Vexilla])]);

        var wardenDatasheet = new Datasheet(
            "Custodian Warden", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Warden", new Statline(6, 6, 2, 5, 7, 2))], weaponProfiles: []);
        var warden = new Unit(wardenDatasheet, [], [new ModelLine("Custodian Warden", [], count: 1)]);

        return new AttachedUnit(bodyguard, [warden]);
    }

    [Fact]
    public void Vexilla_FlagsObjectiveControlOnEveryRowOfTheWholeUnit_NotOnlyTheBearersOwnRow()
    {
        var attachedUnit = CustodianGuardWithVexilla();

        var view = AttachedUnitAggregator.Build(attachedUnit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().HaveCount(2);
        var bodyguardEntry = view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard").Subject;
        bodyguardEntry.Statline.Oc.Value.Should().Be((CharacteristicValue)3);
        // the catalogue's own base OC (2) must survive Vexilla's mutation, not be overwritten by the +1 result
        bodyguardEntry.Statline.Oc.OriginalValue.Should().Be((CharacteristicValue)2);
        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Warden" && s.Statline.Oc.Value == 3);
    }

    [Fact]
    public void Vexilla_UnrelatedCasualtyElsewhereInTheUnit_LeavesTheFlagUntouched()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Attached[0].ModelLines[0].RemoveCasualties(1); // the Warden, not the Vexilla bearer

        var view = AttachedUnitAggregator.Build(attachedUnit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().ContainSingle(s => s.StatlineName == "Custodian Guard" && s.Statline.Oc.Value == 3);
    }

    [Fact]
    public void Vexilla_FlagDisappearsOnceItsBearerModelLineIsFullyRemoved()
    {
        var attachedUnit = CustodianGuardWithVexilla();
        attachedUnit.Bodyguard.ModelLines[0].RemoveCasualties(4);

        var view = AttachedUnitAggregator.Build(attachedUnit, RuleClassificationBaselineFixtures.ShieldDomeAndVexilla);

        view.Statlines.Should().OnlyContain(s => s.Statline.Oc.Value == 2);
    }

    [Fact]
    public void KeywordScopedBaselineMatch_ProducesNoFlaggedValue()
    {
        var keywordScoped = new Ability
        {
            Name = "Faith-Fuelled Resolve",
            Text = "Friendly SWORD BRETHREN SQUAD units have +1 OC.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.OptionalGrant
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: keywordScoped.Text,
                Target: new KeywordRuleTarget("SWORD BRETHREN SQUAD"),
                Effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Sword Brethren Squad", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Sword Brother", [], count: 1, abilities: [keywordScoped])]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.Oc.IsCaveated.Should().BeFalse();
        entry.Statline.Oc.ContributingAbilities.Should().BeEmpty();
        entry.Statline.Oc.Value.Should().Be((CharacteristicValue)1);
    }

    [Fact]
    public void UnconditionallyRosterWideBaselineMatch_ProducesNoFlaggedValue()
    {
        var unconditional = new Ability
        {
            Name = "Some Army-Wide Rule",
            Text = "Add 1 to the Toughness characteristic.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.OptionalGrant
        };
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: unconditional.Text,
                Target: new UnconditionalRuleTarget(),
                Effects: [new ScalarCharacteristicEffect("T", EffectVerb.Improve, 1)])
        ]);
        var datasheet = new Datasheet(
            "Some Unit", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Some Unit", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Some Unit", [], count: 1, abilities: [unconditional])]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.T.IsCaveated.Should().BeFalse();
        entry.Statline.T.ContributingAbilities.Should().BeEmpty();
        entry.Statline.T.Value.Should().Be((CharacteristicValue)4);
    }

    // Same-field stacking between two baseline matches - the first applied wins, the second is
    // skipped. Mirrors CharacteristicModifierApplicationTests' own pinning-test convention for the
    // sibling mechanism.
    [Fact]
    public void TwoBaselineMatchesTargetingTheSameCharacteristic_TheFirstAppliedWins_TheSecondIsSkipped()
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
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(
                Text: abilityA.Text, Target: new SelfRuleTarget(),
                Effects: [new ScalarCharacteristicEffect("W", EffectVerb.Improve, 1)]),
            new RuleClassificationBaselineEntry(
                Text: abilityB.Text, Target: new SelfRuleTarget(),
                Effects: [new ScalarCharacteristicEffect("W", EffectVerb.Improve, 2)])
        ]);
        var datasheet = new Datasheet(
            "Custodian Guard", factionKeywords: [], keywords: [], abilities: [],
            statlines: [("Custodian Guard", new Statline(6, 6, 2, 4, 7, 2))], weaponProfiles: []);
        var unit = new Unit(datasheet, [],
            [new ModelLine("Custodian Guard", [], count: 1, abilities: [abilityA, abilityB])]);

        var view = AttachedUnitAggregator.Build(unit, baseline);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.W.IsCaveated.Should().BeFalse();
        entry.Statline.W.Value.Should().Be((CharacteristicValue)5); // base 4 + First Grant's 1, not + Second's 2
        entry.Statline.W.ContributingAbilities.Should().ContainSingle(a => a.Name == "First Grant");
    }
}