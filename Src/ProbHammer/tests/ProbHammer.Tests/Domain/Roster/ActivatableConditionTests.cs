using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class ActivatableConditionTests
{
    private static readonly RangedWeapon Bolter = new("Boltgun", Range: 24, A: 2, Bs: 3, S: 4, Ap: 0, D: 1);
    private static readonly MeleeWeapon Sword = new("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1);

    private static Ability AbilityWith(string name) => new()
    {
        Name = name, Text = $"{name} rules text.", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic
    };

    private static Unit UnitWith(Ability ability, params WeaponProfile[] weapons) =>
        new(new Datasheet("Squad", keywords: [], abilities: [ability],
                statlines: [("Trooper", new Statline(6, 4, 3, 2, 7, 1))], weaponProfiles: weapons),
            [], [new ModelLine("Trooper", weapons.Select(w => w.Name).ToList(), count: 5)]);

    private static ClassifiedEffect Conditional(RuleEffect effect, string? text, ChoiceBranch? branch = null) => new()
    {
        Effect = effect,
        ResidualConditionBucket = text is null ? ResidualConditionBucket.None : ResidualConditionBucket.Never,
        ConditionText = text,
        ChoiceBranch = branch
    };

    private static AbilityClassificationCatalogue Catalogue(Ability ability, IReadOnlyList<ClassifiedEffect> effects,
        UsageLimit? usageLimit = null, IReadOnlyList<ChoiceGroup>? choiceGroups = null) =>
        ClassificationFixtures.Catalogue(
        [
            (ability.Text, new AbilityClassification
            {
                Target = new SelfRuleTarget(),
                Effects = effects,
                UsageLimit = usageLimit,
                ChoiceGroups = choiceGroups ?? [],
                CoverageStatus = CoverageStatus.Complete
            })
        ]);

    private static WeaponCharacteristicEffect MeleeStrength =>
        new(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1);

    [Fact]
    public void AUsageLimitedAbility_ReportsOneConditionWithNoText()
    {
        var glory = AbilityWith("Chance for Glory");
        var catalogue = Catalogue(glory,
        [
            Conditional(MeleeStrength, null),
            Conditional(new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "D", EffectVerb.Improve, 1),
                null)
        ], UsageLimit.OncePerBattle);

        var view = AttachedUnitAggregator.Build(UnitWith(glory, Sword), catalogue);

        view.ActivatableConditions.Should().ContainSingle().Which.Should()
            .BeEquivalentTo(new ConditionToggle(glory, "", UsageLimit.OncePerBattle, null, IsActive: false));
    }

    [Fact]
    public void DistinctConditionTexts_AreSeparateConditions_AndReportTheirActiveState()
    {
        var firepower = AbilityWith("Advanced Firepower");
        var catalogue = Catalogue(firepower,
        [
            Conditional(new WeaponCharacteristicEffect(new AllWeapons(), "AP", EffectVerb.Improve, 1),
                "Only when targeting an enemy Monster or Vehicle unit"),
            Conditional(new WeaponKeywordGrantEffect(new AllWeapons(), "Sustained Hits 1"),
                "Only when targeting an enemy unit that is not a Monster or Vehicle")
        ]);
        var unit = UnitWith(firepower, Bolter);
        unit.ConditionActivations =
            ActivationFixtures.Condition("Advanced Firepower", "Only when targeting an enemy Monster or Vehicle unit");

        var view = AttachedUnitAggregator.Build(unit, catalogue);

        view.ActivatableConditions.Cast<ConditionToggle>().Select(t => (t.ConditionText, t.IsActive))
            .Should().Equal(
                ("Only when targeting an enemy Monster or Vehicle unit", true),
                ("Only when targeting an enemy unit that is not a Monster or Vehicle", false));
    }

    [Fact]
    public void AChoiceGroup_IsOneCondition_ListingItsOptions()
    {
        var pacts = AbilityWith("Dark Pacts");
        var group = new ChoiceGroup(1, 1, ["[LETHAL HITS]", "[SUSTAINED HITS 1]"]);
        var catalogue = Catalogue(pacts,
        [
            Conditional(new WeaponKeywordGrantEffect(new AllWeapons(), "Lethal Hits"), null, new ChoiceBranch(0, 0)),
            Conditional(new WeaponKeywordGrantEffect(new AllWeapons(), "Sustained Hits 1"), null, new ChoiceBranch(0, 1))
        ], choiceGroups: [group]);
        var unit = UnitWith(pacts, Bolter, Sword);
        unit.ConditionActivations = ActivationFixtures.Choice("Dark Pacts", 0, 1);

        var view = AttachedUnitAggregator.Build(unit, catalogue);

        var toggle = view.ActivatableConditions.Should().ContainSingle().Which.Should().BeOfType<ChoiceToggle>().Subject;
        toggle.Ability.Should().Be(pacts);
        toggle.GroupIndex.Should().Be(0);
        toggle.Group.Should().Be(group);
        toggle.Selected.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public void AConditionThatReachesNothing_IsNotOffered()
    {
        var charge = AbilityWith("Furious Charge");
        var catalogue = Catalogue(charge, [Conditional(MeleeStrength, "if this unit charged")]);

        var view = AttachedUnitAggregator.Build(UnitWith(charge, Bolter), catalogue);

        view.ActivatableConditions.Should().BeEmpty();
    }

    [Fact]
    public void AStatlineCondition_IsOffered()
    {
        var honour = AbilityWith("Martial Honour");
        var catalogue = Catalogue(honour,
            [Conditional(new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 5), "after destroying a unit")]);

        var view = AttachedUnitAggregator.Build(UnitWith(honour), catalogue);

        view.ActivatableConditions.Should().ContainSingle()
            .Which.Should().BeOfType<ConditionToggle>().Which.ConditionText.Should().Be("after destroying a unit");
    }
}
