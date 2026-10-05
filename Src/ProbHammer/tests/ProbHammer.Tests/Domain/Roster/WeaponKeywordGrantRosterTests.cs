using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class WeaponKeywordGrantRosterTests
{
    private static readonly MeleeWeapon PowerSword =
        new("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1) { KeywordsText = ["Assault"] };

    private static Ability AbilityWith(string name, AbilityOrigin origin = AbilityOrigin.Intrinsic) => new()
    {
        Name = name,
        Text = $"{name} rules text.",
        Scope = AbilityScope.Model,
        Origin = origin
    };

    private static AttachedUnit SquadLedBy(Ability? leaderAbility, WeaponProfile? weapon = null)
    {
        weapon ??= PowerSword;
        var bodyguard = new Unit(
            new Datasheet("Sword Brethren Squad", keywords: [], abilities: [],
                statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))], weaponProfiles: [weapon]),
            [], [new ModelLine("Sword Brother", [weapon.Name], count: 4)]);
        var leader = new Unit(
            new Datasheet("Marshal", keywords: [], abilities: leaderAbility is null ? [] : [leaderAbility],
                statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [weapon]),
            [], [new ModelLine("Marshal", [weapon.Name], count: 1)]);
        return new AttachedUnit(bodyguard, [leader]);
    }

    private static AbilityClassificationCatalogue Grants(Ability ability, RuleTarget target, string keyword,
        bool conditional = false, WeaponSelector? selector = null) =>
        ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(ability.Text, target,
                [new WeaponKeywordGrantEffect(selector ?? new WeaponClass(WeaponType.Melee), keyword)], conditional)
        ]);

    [Fact]
    public void AUnitWideKeywordGrant_KeepsEveryReachedContributorMerged()
    {
        var banner = AbilityWith("War Banner");
        var view = AttachedUnitAggregator.Build(SquadLedBy(banner),
            Grants(banner, new AttachedUnitRuleTarget(), "Lethal Hits"));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Assault", "Lethal Hits");
        entry.Contributions.Should().HaveCount(2)
            .And.OnlyContain(c => c.KeywordGrants.Single().SourceAbility == banner);
        entry.KeywordGrants.Should().ContainSingle(g => g.Keyword == "Lethal Hits" && g.ReplacedKeyword == null);
    }

    [Fact]
    public void ABearerScopedKeywordGrant_SplitsAnOtherwiseMergedGroup()
    {
        var blade = AbilityWith("Blessed Blade");
        var view = AttachedUnitAggregator.Build(SquadLedBy(blade),
            Grants(blade, new SelfRuleTarget(), "Lethal Hits"));

        view.Weapons.Should().HaveCount(2);
        view.Weapons.Single(w => w.Contributions.Single().ComponentName == "Marshal")
            .Profile.KeywordsText.Should().Equal("Assault", "Lethal Hits");
        view.Weapons.Single(w => w.Contributions.Single().ComponentName == "Sword Brethren Squad")
            .Profile.KeywordsText.Should().Equal("Assault");
    }

    [Fact]
    public void AGrantTheWeaponAlreadyHas_IsNotRecorded()
    {
        var banner = AbilityWith("War Banner");
        var weapon = PowerSword with { KeywordsText = ["Lethal Hits"] };
        var view = AttachedUnitAggregator.Build(SquadLedBy(banner, weapon),
            Grants(banner, new AttachedUnitRuleTarget(), "LETHAL HITS"));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Lethal Hits");
        entry.KeywordGrants.Should().BeEmpty();
        entry.Contributions.Should().OnlyContain(c => c.KeywordGrants.Count == 0);
    }

    [Fact]
    public void ABetterValueGrant_ReplacesTheWeaponsOwnKeyword_AndRecordsTheReplacement()
    {
        var banner = AbilityWith("War Banner");
        var weapon = PowerSword with { KeywordsText = ["Sustained Hits 1"] };
        var view = AttachedUnitAggregator.Build(SquadLedBy(banner, weapon),
            Grants(banner, new AttachedUnitRuleTarget(), "Sustained Hits 2"));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Sustained Hits 2");
        entry.KeywordGrants.Should().ContainSingle()
            .Which.ReplacedKeyword.Should().Be("Sustained Hits 1");
    }

    [Fact]
    public void ADetachmentRuleAbility_ReachesEveryComponentsWeapons()
    {
        var rule = AbilityWith("Rapid Assault", AbilityOrigin.DetachmentRule);
        var unit = SquadLedBy(null, PowerSword with { KeywordsText = [] });
        unit.InboundAbilities = [rule];

        var view = AttachedUnitAggregator.Build(unit,
            Grants(rule, new KeywordRuleTarget(["ADEPTUS ASTARTES"]), "Assault", selector: new AllWeapons()));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Assault");
        entry.Contributions.Should().HaveCount(2).And.OnlyContain(c => c.KeywordGrants.Count == 1);
    }

    [Fact]
    public void ADetachmentRuleKeywordTargetedStrengthEffect_MutatesEveryComponentsWeapon()
    {
        var rule = AbilityWith("Crusade of Wrath", AbilityOrigin.DetachmentRule);
        var unit = SquadLedBy(null);
        unit.InboundAbilities = [rule];
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(rule.Text, new KeywordRuleTarget(["ADEPTUS ASTARTES"]),
                [new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve, 1)])
        ]);

        var view = AttachedUnitAggregator.Build(unit, classifications);

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Contributions.Should().HaveCount(2);
        entry.Profile.S.Value.Should().Be((CharacteristicValue)5);
    }

    [Fact]
    public void AKeywordTargetedGrantFromANonDetachmentAbility_ReachesNoWeapon()
    {
        var aura = AbilityWith("Aura");
        var view = AttachedUnitAggregator.Build(SquadLedBy(aura),
            Grants(aura, new KeywordRuleTarget(["INFANTRY"]), "Lethal Hits"));

        view.Weapons.Should().ContainSingle().Which.KeywordGrants.Should().BeEmpty();
    }

    [Fact]
    public void AConditionalGrantReachingSomeContributors_KeepsTheEntryMerged_AndIsReportedAtEntryLevel()
    {
        var charge = AbilityWith("Glorious Charge");
        var view = AttachedUnitAggregator.Build(SquadLedBy(charge),
            Grants(charge, new SelfRuleTarget(), "Lance", conditional: true));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Assault");
        entry.KeywordGrants.Should().BeEmpty();
        var notApplied = entry.NotAppliedKeywordGrants.Should().ContainSingle().Subject;
        notApplied.Keyword.Should().Be("Lance");
        notApplied.SourceAbility.Should().Be(charge);
        notApplied.Condition.ConditionText.Should().Be("Test condition");
        entry.Contributions.Single(c => c.ComponentName == "Marshal").NotAppliedKeywordGrants.Should().ContainSingle();
        entry.Contributions.Single(c => c.ComponentName == "Sword Brethren Squad").NotAppliedKeywordGrants
            .Should().BeEmpty();
    }

    [Fact]
    public void AnActivatedGrantReachingSomeContributors_SplitsTheMergedRow()
    {
        var charge = AbilityWith("Glorious Charge");
        var unit = SquadLedBy(charge);
        unit.ConditionActivations = ActivationFixtures.Condition("Glorious Charge", "Test condition");

        var view = AttachedUnitAggregator.Build(unit, Grants(charge, new SelfRuleTarget(), "Lance", conditional: true));

        view.Weapons.Should().HaveCount(2).And.OnlyContain(w => w.NotAppliedKeywordGrants.Count == 0);
        var marshal = view.Weapons.Single(w => w.Contributions.Single().ComponentName == "Marshal");
        marshal.Profile.KeywordsText.Should().Equal("Assault", "Lance");
        marshal.KeywordGrants.Should().ContainSingle(g => g.SourceAbility == charge);
    }

    [Fact]
    public void AnActivatedGrant_IsNoLongerAppliedOnceItsBearerIsDestroyed()
    {
        var charge = AbilityWith("Glorious Charge");
        var unit = SquadLedBy(charge);
        unit.ConditionActivations = ActivationFixtures.Condition("Glorious Charge", "Test condition");
        unit.Attached[0].ModelLines[0].RemoveCasualties(1);

        var view = AttachedUnitAggregator.Build(unit, Grants(charge, new SelfRuleTarget(), "Lance", conditional: true));

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Assault");
        entry.KeywordGrants.Should().BeEmpty();
        unit.ConditionActivations.For("Glorious Charge").Should().NotBeNull();
    }

    private static readonly Ability DarkPacts = AbilityWith("Dark Pacts");

    private static AbilityClassificationCatalogue DarkPactsCatalogue() => ClassificationFixtures.Catalogue(
    [
        (DarkPacts.Text, new AbilityClassification
        {
            Target = new AttachedUnitRuleTarget(),
            Effects =
            [
                new ClassifiedEffect
                {
                    Effect = new WeaponKeywordGrantEffect(new AllWeapons(), "Lethal Hits"),
                    ResidualConditionBucket = ResidualConditionBucket.None, ChoiceBranch = new ChoiceBranch(0, 0)
                },
                new ClassifiedEffect
                {
                    Effect = new WeaponKeywordGrantEffect(new AllWeapons(), "Sustained Hits 1"),
                    ResidualConditionBucket = ResidualConditionBucket.None, ChoiceBranch = new ChoiceBranch(0, 1)
                }
            ],
            ChoiceGroups = [new ChoiceGroup(1, 1, ["[LETHAL HITS]", "[SUSTAINED HITS 1]"])],
            CoverageStatus = CoverageStatus.Complete
        })
    ]);

    [Fact]
    public void DarkPactsWithLethalHitsSelected_AppliesItsGrant_AndDropsTheOtherOption()
    {
        var unit = SquadLedBy(DarkPacts);
        unit.ConditionActivations = ActivationFixtures.Choice("Dark Pacts", 0, 0);

        var view = AttachedUnitAggregator.Build(unit, DarkPactsCatalogue());

        var entry = view.Weapons.Should().ContainSingle().Subject;
        entry.Profile.KeywordsText.Should().Equal("Assault", "Lethal Hits");
        entry.KeywordGrants.Should().ContainSingle(g => g.Keyword == "Lethal Hits");
        entry.NotAppliedKeywordGrants.Should().BeEmpty();
    }

    [Fact]
    public void DarkPactsWithNoSelection_RecordsBothOptionsAsNotApplied()
    {
        var view = AttachedUnitAggregator.Build(SquadLedBy(DarkPacts), DarkPactsCatalogue());

        view.Weapons.Should().ContainSingle().Which.NotAppliedKeywordGrants.Select(g => g.Keyword)
            .Should().Equal("Lethal Hits", "Sustained Hits 1");
    }

    [Fact]
    public void AConditionalRedundantGrant_IsNotRecorded()
    {
        var charge = AbilityWith("Glorious Charge");
        var view = AttachedUnitAggregator.Build(SquadLedBy(charge),
            Grants(charge, new SelfRuleTarget(), "ASSAULT", conditional: true));

        view.Weapons.Should().ContainSingle().Which.NotAppliedKeywordGrants.Should().BeEmpty();
    }
}