using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Real-corpus verification - drives the full production pipeline
/// (BsdataFactionResolver -> ResolvedBsdataCatalogue -> ArmyRosterEnricher.Enrich ->
/// AttachedUnitAggregator.Build) against the real bundled BSData snapshot
/// (`src/ProbHammer.Web/BsData/`, the same data the running app reads) and the real checked-in
/// `RuleClassificationBaseline` (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`), not a
/// hand-built fixture or a mocked baseline - the same production inputs `/LivePlay` uses.
///
/// Covers two scenarios needing hard, non-optional real-corpus confirmation: an InSv-footnote unit
/// (Space Marines' Judiciar - a melee-only invulnerable-save grant, resolved via
/// AttachedUnitAggregator's Build-time baseline resolution) and an Auric-Mantle-shaped Enhancement
/// (Adeptus Custodes' Shield-Captain, structurally classified as a CharacteristicModifierCandidate
/// AND baseline-matched by its own Ability text - the one case where both classification paths
/// apply to the same ability, resolved through a single unified pass with no separate coordination
/// mechanism involved).</summary>
public class UnifiedCharacteristicEffectResolutionRegressionTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web", "BsData"));

    private static string BundledBaselinePath([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "Data", "RuleEffectClassifications.json"));

    private static ArmyRoster EnrichStandaloneUnit(
        IReadOnlyList<string> faction, string unitName, IReadOnlyList<string> weapons,
        IReadOnlyList<string> enhancements)
    {
        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: faction, Detachments: [], ForceDisposition: "Test",
            BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits: [new ParsedUnit(unitName, [new ParsedModelGroup(unitName, 1, weapons)], enhancements)]);

        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        return ArmyRosterEnricher.Enrich(parsed, catalogue);
    }

    [Fact]
    public void Judiciar_MeleeOnlyInvulnerableSaveFootnote_ResolvesViaTheRealCheckedInBaseline()
    {
        // Judiciar's own raw InSv text ("4+*") is a bare footnote naming a linked ability whose
        // real text ("This model has a 4+ invulnerable save against melee attacks.") the mapper
        // does not interpret itself - it stays caveated at parse time and gets exactly one
        // resolution attempt against the baseline during roster aggregation. The real checked-in
        // baseline already carries this exact text, so this must resolve to a real melee=4/ranged=0
        // split, not stay caveated.
        var roster = EnrichStandaloneUnit(["Space Marines"], "Judiciar", weapons: [], enhancements: []);
        var judiciar = roster.Units.Single();
        var baseline = RuleClassificationBaseline.Load(BundledBaselinePath());

        var view = AttachedUnitAggregator.Build(judiciar, baseline);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.Value.MeleeInSv.Should().Be(4);
        entry.Statline.InSv.Value.RangedInSv.Should().Be(0);
    }

    [Fact]
    public void ShieldCaptainWithAuricMantle_ResolvesWoundsThroughTheUnifiedPassAlone()
    {
        // Auric Mantle is both a real, tier-1 CharacteristicModifierCandidate (Improve W by 2,
        // structurally classified from BSData's own modifiers) AND a baseline-matched Ability text
        // ("Add 2 to the bearer's Wounds characteristic.") - the one case where both classification
        // paths apply to the same ability. Confirms it lands on the correct, resolved (not
        // caveated) value with no separate coordination mechanism involved.
        var roster = EnrichStandaloneUnit(
            ["Imperium", "Adeptus Custodes"], "Shield-Captain", weapons: [], enhancements: ["Auric Mantle"]);
        var shieldCaptain = roster.Units.Single();
        var baseline = RuleClassificationBaseline.Load(BundledBaselinePath());

        var view = AttachedUnitAggregator.Build(shieldCaptain, baseline);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        var baseWounds = ((NumericCharacteristicValue)entry.Statline.W.OriginalValue).Value;
        entry.Statline.W.IsCaveated.Should().BeFalse();
        ((NumericCharacteristicValue)entry.Statline.W.Value).Value.Should().Be(baseWounds + 2);
        entry.Statline.W.ContributingAbilities.Should().ContainSingle(a => a.Name == "Auric Mantle");
    }
}
