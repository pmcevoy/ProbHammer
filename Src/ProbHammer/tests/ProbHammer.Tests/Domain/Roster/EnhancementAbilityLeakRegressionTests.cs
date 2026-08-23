using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Regression coverage for a real user-reported bug, traced against the real bundled
/// BSData snapshot (`src/ProbHammer.Web/BsData/`, not a hand-built fixture) using a real captured
/// export: after importing `data/gw-app-export-templars.txt`, Black Templars' "Crusade Ancient"
/// incorrectly showed "Thirst for Glory" (a Space-Wolves-only Enhancement) and "Sword Brethren
/// Squad" incorrectly showed both "Fervent Exemplars" and "Inheritors of Sigismund" (two real
/// Black Templars Enhancements) - none selected in the export. Root cause and fix: see this
/// change's proposal.md/design.md ("Datasheet.Abilities never got the same on-demand-only
/// protection WeaponProfile already has").</summary>
public class EnhancementAbilityLeakRegressionTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web", "BsData"));

    private static string RealExportText([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "data", "gw-app-export-templars.txt"));

    private static ProbHammer.Core.Domain.Roster.ArmyRoster EnrichRealExport()
    {
        var parsed = new ArmyListParser().Parse(RealExportText());
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        return ProbHammer.Core.Domain.Roster.ArmyRosterEnricher.Enrich(parsed, catalogue);
    }

    [Fact]
    public void CrusadeAncient_DoesNotShowTheUnselectedSpaceWolvesEnhancement()
    {
        var roster = EnrichRealExport();

        var crusadeAncient = roster.Units
            .SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Crusade Ancient");

        crusadeAncient.Datasheet.Abilities.Should().NotContain(a => a.Name == "Thirst for Glory");
    }

    [Fact]
    public void CrusadeAncient_HasNoEnhancementAttached()
    {
        // The real export selects no Enhancement for Crusade Ancient at all.
        var roster = EnrichRealExport();

        var crusadeAncient = roster.Units
            .SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Crusade Ancient");

        crusadeAncient.Enhancements.Should().BeEmpty();
    }

    [Fact]
    public void SwordBrethrenSquad_DoesNotShowEitherUnselectedEnhancement()
    {
        var roster = EnrichRealExport();

        var swordBrethren = roster.Units
            .SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Sword Brethren Squad");

        swordBrethren.Datasheet.Abilities.Should().NotContain(a => a.Name == "Fervent Exemplars");
        swordBrethren.Datasheet.Abilities.Should().NotContain(a => a.Name == "Inheritors of Sigismund");
    }

    [Fact]
    public void Impulsor_ShieldDomeWargearLine_StillResolvesAsAModelLineAbility()
    {
        // This real export's Impulsor selects "1x Shield Dome" as a direct wargear line -
        // confirms task 4.1's repoint (ResolveWargearItem's ability fallback onto
        // TryResolveAbility) still works end-to-end against the real bundled BSData snapshot, not
        // just the hand-built fixture in ArmyRosterEnricherTests.
        var roster = EnrichRealExport();

        var impulsor = roster.Units
            .SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Impulsor" || u.Datasheet.Name.EndsWith(" Impulsor"));

        impulsor.ModelLines.Should().ContainSingle(ml => ml.Abilities.Any(a => a.Name == "Shield Dome" && !string.IsNullOrWhiteSpace(a.Text)));
    }
}
