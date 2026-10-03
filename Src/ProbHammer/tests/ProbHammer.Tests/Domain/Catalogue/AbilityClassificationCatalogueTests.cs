using ProbHammer.Tests.Domain.Fixtures;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class AbilityClassificationCatalogueTests
{
    private const string EveryKindJson = """
                                         {
                                           "records": [
                                             {
                                               "hash": "h1",
                                               "text": "irrelevant",
                                               "names": [ "Every Kind" ],
                                               "classification": {
                                                 "target": { "kind": "Keyword", "keywords": [ "LEAGUES OF VOTANN", "INFANTRY" ] },
                                                 "effects": [
                                                   { "effect": { "kind": "Scalar", "characteristic": "Oc", "verb": "improve", "amount": 1 },
                                                     "residualConditionBucket": "none" },
                                                   { "effect": { "kind": "InvulnerableSave", "value": { "meleeInSv": 4, "rangedInSv": 4 } },
                                                     "residualConditionBucket": "evaluable-now", "conditionText": "While below half strength" },
                                                   { "effect": { "kind": "WeaponCharacteristic", "selector": { "kind": "WeaponClass", "type": "Ranged" },
                                                     "characteristic": "S", "verb": "improve", "amount": 2 },
                                                     "residualConditionBucket": "never", "conditionText": "While targeting the closest unit",
                                                     "choiceBranch": { "group": 0, "option": 1 } },
                                                   { "effect": { "kind": "FeelNoPain", "value": 5, "qualifier": "Mortal Wounds" },
                                                     "residualConditionBucket": "none" },
                                                   { "effect": { "kind": "WeaponKeywordGrant", "selector": { "kind": "AllWeapons" },
                                                     "keyword": "Sustained Hits 1", "replacesKeyword": "Sustained Hits" },
                                                     "residualConditionBucket": "none" },
                                                   { "effect": { "kind": "NamedAbilityGrant", "abilityName": "Scouts 9\"" },
                                                     "residualConditionBucket": "none" },
                                                   { "effect": { "kind": "NamedAbilityRemoval", "abilityName": "Dark Pacts" },
                                                     "residualConditionBucket": "none" }
                                                 ],
                                                 "choiceGroups": [ { "minSelect": 1, "maxSelect": 1, "options": [ "A", "B" ] } ],
                                                 "phases": [ "command", "shooting", "fight" ],
                                                 "turnOwnership": "theirs",
                                                 "usageLimit": "Once per battle round",
                                                 "coverageStatus": "partial",
                                                 "unclassifiedResidue": "Something else"
                                               }
                                             }
                                           ]
                                         }
                                         """;

    private static string WriteTemp(string json)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        return path;
    }

    private static AbilityClassification Load(string json) =>
        AbilityClassificationCatalogue.Load(WriteTemp(json)).Records.Single().Classification;

    [Fact]
    public void AMissingFile_LoadsAsAnEmptyCatalogue()
    {
        var catalogue = AbilityClassificationCatalogue.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        catalogue.Records.Should().BeEmpty();
    }

    [Fact]
    public void EveryEffectKind_Loads()
    {
        var effects = Load(EveryKindJson).Effects.Select(e => e.Effect).ToList();

        effects.Should().Equal(
            new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1),
            new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(4, 4)),
            new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Ranged), "S", EffectVerb.Improve, 2),
            new FeelNoPainEffect(5, "Mortal Wounds"),
            new WeaponKeywordGrantEffect(new AllWeapons(), "Sustained Hits 1", "Sustained Hits"),
            new NamedAbilityGrantEffect("Scouts 9\""),
            new NamedAbilityRemovalEffect("Dark Pacts"));
    }

    [Fact]
    public void EveryTopLevelField_Loads()
    {
        var classification = Load(EveryKindJson);

        classification.Target.Should().Be(new KeywordRuleTarget(["LEAGUES OF VOTANN", "INFANTRY"]));
        classification.Phases.Should().Equal(GamePhase.Command, GamePhase.Shooting, GamePhase.Fight);
        classification.TurnOwnership.Should().Be(GameTurn.Theirs);
        classification.UsageLimit.Should().Be(UsageLimit.OncePerBattleRound);
        classification.CoverageStatus.Should().Be(CoverageStatus.Partial);
        classification.UnclassifiedResidue.Should().Be("Something else");
        classification.ChoiceGroups.Should().ContainSingle()
            .Which.Options.Should().Equal("A", "B");
        classification.Effects[1].ResidualConditionBucket.Should().Be(ResidualConditionBucket.EvaluableNow);
        classification.Effects[1].ConditionText.Should().Be("While below half strength");
        classification.Effects[2].ChoiceBranch.Should().Be(new ChoiceBranch(0, 1));
    }

    [Fact]
    public void AnUnrecognizedEffectKind_FailsTheLoad()
    {
        var json = EveryKindJson.Replace("\"kind\": \"FeelNoPain\"", "\"kind\": \"Teleport\"");

        var load = () => AbilityClassificationCatalogue.Load(WriteTemp(json));

        load.Should().Throw<Exception>();
    }

    [Fact]
    public void Lookup_FoldsATypographicApostropheAndANonBreakingSpace()
    {
        var classification = Load(EveryKindJson);
        var catalogue = AbilityClassificationCatalogue.FromTexts(
            [("Add 1 to the bearer’s unit Objective Control.", classification)]);

        catalogue.TryGet("Add 1 to the bearer's unit Objective Control.", out var found).Should().BeTrue();
        found.Should().BeSameAs(classification);
    }

    [Fact]
    public void TheCheckedInCatalogue_LoadsAndEveryKeyIsTheHashOfItsOwnText()
    {
        var catalogue = AbilityClassificationCatalogue.Load(ClassificationFixtures.CheckedInCataloguePath());

        catalogue.Records.Should().NotBeEmpty();
        catalogue.Records.Should().OnlyContain(r => r.Hash == AbilityTextKey.Hash(r.Text));
    }

    [Fact]
    public void Lookup_OfAnUnclassifiedText_FindsNothing()
    {
        AbilityClassificationCatalogue.Empty.TryGet("Some text", out _).Should().BeFalse();
    }
}