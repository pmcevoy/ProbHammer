using ProbHammer.Tests.Domain.Fixtures;
using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Drives the full production pipeline (BsdataFactionResolver -> ResolvedBsdataCatalogue ->
/// ArmyRosterEnricher.Enrich -> AttachedUnitAggregator.Build) against the real bundled BSData
/// snapshot, for a melee-only invulnerable-save footnote (Judiciar) and a Wounds-granting
/// Enhancement (Auric Mantle), resolving against the checked-in catalogue.</summary>
public class CharacteristicEffectRealCorpusTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "BsData"));

    private static ArmyRoster EnrichStandaloneUnit(
        IReadOnlyList<string> faction, string unitName, IReadOnlyList<string> weapons,
        IReadOnlyList<string> enhancements, string? modelName = null)
    {
        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: faction, Detachments: [], ForceDisposition: "Test",
            BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits: [new ParsedUnit(unitName, [new ParsedModelGroup(modelName ?? unitName, 1, weapons)], enhancements)]);

        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        return ArmyRosterEnricher.Enrich(parsed, catalogue);
    }

    [Fact]
    public void Judiciar_MeleeOnlyInvulnerableSaveFootnote_ResolvesViaItsClassification()
    {
        // Judiciar's raw InSv ("4+*") is a footnote the mapper leaves caveated; its linked ability's
        // classification must resolve it to melee 4+ only.
        var roster = EnrichStandaloneUnit(["Space Marines"], "Judiciar", weapons: [], enhancements: []);
        var judiciar = roster.Units.Single();
        var classifications = ClassificationFixtures.CheckedIn;

        var view = AttachedUnitAggregator.Build(judiciar, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        entry.Statline.InSv.IsCaveated.Should().BeFalse();
        entry.Statline.InSv.Value.MeleeInSv.Should().Be(4);
        entry.Statline.InSv.Value.RangedInSv.Should().Be(0);
    }

    [Fact]
    public void HowlingBanshees_SplitInvulnerableSaveFootnote_KeepsThePlainRangedSave()
    {
        // "4+* / 5+": the footnote names melee 4+ only, so ranged keeps the plain 5+.
        var roster = EnrichStandaloneUnit(
            ["Aeldari", "Craftworlds"], "Howling Banshees", weapons: [], enhancements: [], modelName: "Howling Banshee");
        var banshees = roster.Units.Single();

        var view = AttachedUnitAggregator.Build(banshees, ClassificationFixtures.CheckedIn);

        view.Statlines.Should().NotBeEmpty().And.OnlyContain(e =>
            !e.Statline.InSv.IsCaveated && e.Statline.InSv.Value == new InvulnerableSave(4, 5));
    }

    [Fact]
    public void ShieldCaptainWithAuricMantle_ResolvesWounds()
    {
        var roster = EnrichStandaloneUnit(
            ["Imperium", "Adeptus Custodes"], "Shield-Captain", weapons: [], enhancements: ["Auric Mantle"]);
        var shieldCaptain = roster.Units.Single();
        var classifications = ClassificationFixtures.CheckedIn;

        var view = AttachedUnitAggregator.Build(shieldCaptain, classifications);

        var entry = view.Statlines.Should().ContainSingle().Subject;
        var baseWounds = ((NumericCharacteristicValue)entry.Statline.W.OriginalValue).Value;
        entry.Statline.W.IsCaveated.Should().BeFalse();
        ((NumericCharacteristicValue)entry.Statline.W.Value).Value.Should().Be(baseWounds + 2);
        entry.Statline.W.ContributingAbilities.Should().ContainSingle(a => a.Name == "Auric Mantle");
    }
}