using System.Text.Json.Nodes;
using FluentAssertions;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Tests;

public class CatalogueExporterTests
{
    [Fact]
    public void ARollModifiersNamedWeapon_IsExportedCanonically()
    {
        var classification = Fixtures.Classification(new RollModifierEffect(
            ModifiedRoll.Hit, RollOwner.Friendly, RollModifierKind.RerollOnes, 0, WeaponName: "heavy bolters"));
        var resolver = new NameResolver(Fixtures.Vocabulary(weaponNames: ["Heavy bolter"]));
        var record = Fixtures.Record("h1", classification) with { Resolution = resolver.Resolve(classification) };

        var exported = JsonNode.Parse(Export(Corpus(record), record).Json!)!;

        var effect = exported["records"]![0]!["classification"]!["effects"]![0]!["effect"]!;
        effect["kind"]!.GetValue<string>().Should().Be("RollModifier");
        effect["selector"]!["name"]!.GetValue<string>().Should().Be("Heavy bolter");
    }

    [Theory]
    [InlineData(ModifiedRoll.Hit, AttackWeaponClass.Melee, """{"kind":"WeaponClass","type":"Melee"}""")]
    [InlineData(ModifiedRoll.Damage, null, """{"kind":"AllWeapons"}""")]
    [InlineData(ModifiedRoll.Charge, null, null)]
    public void ARollModifiersFlatWeaponFields_AreExportedAsCoresSelector(ModifiedRoll roll,
        AttackWeaponClass? weaponClass, string? expectedSelector)
    {
        var record = Fixtures.Record("h1", Fixtures.Classification(
            new RollModifierEffect(roll, RollOwner.Enemy, RollModifierKind.Subtract, 1, weaponClass)));

        var effect = JsonNode.Parse(Export(Corpus(record), record).Json!)!
            ["records"]![0]!["classification"]!["effects"]![0]!["effect"]!.AsObject();

        effect["selector"]?.ToJsonString().Should().Be(expectedSelector);
        effect.ContainsKey("weaponClass").Should().BeFalse();
    }

    [Fact]
    public void AClassificationOfAStaleText_IsLeftOutAndCounted()
    {
        var live = Fixtures.Record("live", Fixtures.Classification());
        var stale = Fixtures.Record("stale", Fixtures.Classification());
        var corpus = Corpus(live);
        corpus["stale"] = Fixtures.Extraction("stale", stale.Text, Fixtures.Today.AddDays(-23));
        var classifications = new Dictionary<string, ClassificationRecord> { ["live"] = live, ["stale"] = stale };

        var export = CatalogueExporter.Export(classifications, corpus);

        export.LeftOut.Should().Be(1);
        JsonNode.Parse(export.Json!)!["records"]!.AsArray().Select(r => r!["hash"]!.GetValue<string>())
            .Should().Equal("live");
        classifications.Keys.Should().BeEquivalentTo("live", "stale");
    }

    [Fact]
    public void ACarriageReturnInConditionText_FailsTheExportNamingTheHashAndField()
    {
        var record = Fixtures.Record("h1", Fixtures.Classification() with
        {
            Effects =
            [
                new ClassifiedEffect
                {
                    Effect = new NamedAbilityGrantEffect("Stealth"),
                    ResidualConditionBucket = ResidualConditionBucket.Never,
                    ConditionText = "While the bearer\rs unit is in cover"
                }
            ]
        });

        var export = Export(Corpus(record), record);

        export.Json.Should().BeNull();
        export.ControlCharacters.Should().Equal("h1 classification.effects[0].conditionText");
    }

    [Fact]
    public void ALineFeed_IsAllowed()
    {
        var record = Fixtures.Record("h1", Fixtures.Classification() with { UnclassifiedResidue = "One.\nTwo." });

        Export(Corpus(record), record).Json.Should().NotBeNull();
    }

    private static Dictionary<string, ExtractionRecord> Corpus(params ClassificationRecord[] records) =>
        records.ToDictionary(r => r.Hash, r => Fixtures.Extraction(r.Hash, r.Text));

    private static CatalogueExport Export(Dictionary<string, ExtractionRecord> corpus,
        params ClassificationRecord[] records) =>
        CatalogueExporter.Export(records.ToDictionary(r => r.Hash), corpus);
}