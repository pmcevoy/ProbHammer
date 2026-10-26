using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Regression coverage for a real user-reported bug, traced against the real bundled
/// BSData snapshot (`src/ProbHammer.Web/BsData/`, not a hand-built fixture) using a real captured
/// export: Black Templars units showed Enhancements the export never selected for them. Root cause:
/// Datasheet.Abilities never got the same on-demand-only protection WeaponProfile already has.</summary>
public class EnhancementAbilityLeakRegressionTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "BsData"));

    private static string RealExportText([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "data",
            "gw-templars-for-unit-tests.txt"));

    private static ProbHammer.Core.Domain.Roster.ArmyRoster EnrichRealExport()
    {
        var parsed = new ArmyListParser().Parse(RealExportText());
        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var fileName = BsdataFactionResolver.ResolveStartingFileName(parsed.Faction, source.ListFileNames());
        var catalogue = ResolvedBsdataCatalogue.Build(source, fileName);
        return ProbHammer.Core.Domain.Roster.ArmyRosterEnricher.Enrich(parsed, catalogue);
    }

    private static ProbHammer.Core.Domain.Roster.Unit Component(string datasheetName) =>
        EnrichRealExport().Units.SelectMany(u => u.Components).Single(u => u.Datasheet.Name == datasheetName);

    [Fact]
    public void Castellan_ShowsItsSelectedEnhancement()
    {
        Component("Castellan").Enhancements.Should().ContainSingle(e => e.Name == "Laurels of Vigilance");
    }

    [Fact]
    public void CrusadeAncient_HasNoEnhancementAttached()
    {
        var crusadeAncient = Component("Crusade Ancient");

        crusadeAncient.Enhancements.Should().BeEmpty();
        crusadeAncient.Datasheet.Abilities.Should().NotContain(a => a.Name == "Laurels of Vigilance");
    }

    [Fact]
    public void SwordBrethrenSquad_DoesNotShowEitherUnselectedEnhancement()
    {
        var swordBrethren = Component("Sword Brethren Squad");

        swordBrethren.Datasheet.Abilities.Should().NotContain(a => a.Name == "Fervent Exemplars");
        swordBrethren.Datasheet.Abilities.Should().NotContain(a => a.Name == "Inheritors of Sigismund");
    }

    [Fact]
    public void Impulsor_ShieldDomeWargearLine_StillResolvesAsAModelLineAbility()
    {
        // The Impulsor's "1x Shield Dome" wargear line resolves through ResolveWargearItem's ability
        // fallback against the real bundled BSData, not just ArmyRosterEnricherTests' hand-built fixture.
        var impulsor = EnrichRealExport().Units
            .SelectMany(u => u.Components)
            .Single(u => u.Datasheet.Name == "Impulsor" || u.Datasheet.Name.EndsWith(" Impulsor"));

        impulsor.ModelLines.Should().ContainSingle(ml =>
            ml.Abilities.Any(a => a.Name == "Shield Dome" && !string.IsNullOrWhiteSpace(a.Text)));
    }
}