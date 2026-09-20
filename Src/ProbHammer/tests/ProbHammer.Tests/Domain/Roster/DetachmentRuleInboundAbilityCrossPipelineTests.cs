using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers `army-roster-enrichment`'s "Both import pipelines produce identical
/// InboundAbilities for equivalent input" scenario: the same Detachment + matching-keyword-unit
/// shape, imported once via each pipeline, run through
/// <see cref="DetachmentRuleInboundAbilityResolver"/> with the same baseline.</summary>
public class DetachmentRuleInboundAbilityCrossPipelineTests
{
    private const string FaithFuelledResolveText = "Friendly SWORD BRETHREN SQUAD units have +1 OC.";

    private static readonly RuleClassificationBaseline Baseline = RuleClassificationBaseline.FromEntries(
    [
        new RuleClassificationBaselineEntry(
            Text: FaithFuelledResolveText,
            Target: new KeywordRuleTarget("SWORD BRETHREN SQUAD"),
            Effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
    ]);

    // Own CallerFilePath-based lookup rather than reusing BsdataFixtures.Source() - that helper's
    // relative "../.." climb is tuned to its own callers' fixed nesting depth (Domain/Catalogue/
    // Bsdata), which this file (Domain/Roster) does not share.
    private static LocalDiskBsdataCatalogueSource BsdataFixturesSource([CallerFilePath] string here = "") =>
        new(Path.Combine(Path.GetDirectoryName(here)!, "..", "Fixtures", "Bsdata"));

    private static ArmyRoster BuildTextPipelineRoster()
    {
        var catalogue = ResolvedBsdataCatalogue.Build(BsdataFixturesSource(), "crusader-squad-enrichment.json");
        var parsed = new ParsedArmyList(
            Name: "Test Army",
            PointsSpent: 500,
            Faction: ["Imperium", "Black Templars"],
            Detachments: ["Test Detachment"],
            ForceDisposition: "Test Disposition",
            BattleSize: "Incursion",
            PointsLimit: 1000,
            AttachmentGroups: [],
            StandaloneUnits:
            [
                new ParsedUnit(
                    Name: "Crusader Squad",
                    ModelGroups:
                    [new ParsedModelGroup("Sword Brother", 1, ["Master-crafted power weapon", "Pyre pistol"])],
                    Enhancements: [])
            ]);

        return ArmyRosterEnricher.Enrich(parsed, catalogue);
    }

    private static ArmyRoster BuildBattleScribePipelineRoster([CallerFilePath] string here = "")
    {
        var json = File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "Import", "BattleScribe", "Fixtures",
            "detachment-rule-keyword-target-excerpt.json"));
        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeTrue();
        return BattleScribeRosterMapper.Map(roster!);
    }

    [Fact]
    public void BothPipelines_ProduceIdenticalInboundAbilities_ForEquivalentInput()
    {
        var textRoster = BuildTextPipelineRoster();
        var battleScribeRoster = BuildBattleScribePipelineRoster();

        DetachmentRuleInboundAbilityResolver.Apply(textRoster.Units, textRoster.Detachments, Baseline);
        DetachmentRuleInboundAbilityResolver.Apply(battleScribeRoster.Units, battleScribeRoster.Detachments, Baseline);

        var textUnit = textRoster.Units.Should().ContainSingle().Subject;
        var battleScribeUnit = battleScribeRoster.Units.Should().ContainSingle().Subject;

        textUnit.InboundAbilities.Should().ContainSingle(a =>
            a.Name == "Faith-Fuelled Resolve" &&
            a.Text == FaithFuelledResolveText &&
            a.Scope == AbilityScope.Unit &&
            a.Origin == AbilityOrigin.DetachmentRule);

        battleScribeUnit.InboundAbilities.Should().BeEquivalentTo(textUnit.InboundAbilities);
    }
}