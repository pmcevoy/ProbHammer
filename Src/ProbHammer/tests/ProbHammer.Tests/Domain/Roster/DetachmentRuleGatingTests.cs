using System.Runtime.CompilerServices;
using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Every Space Marine datasheet links Templar Vows, the Vow-Sworn Crusaders Detachment rule,
/// so a unit should only carry it when the army chose that Detachment.</summary>
public class DetachmentRuleGatingTests
{
    private static string BundledBsDataRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web", "BsData"));

    private static Unit IntercessorSquad(string detachment)
    {
        var faction = new[] { "Space Marines", "Black Templars" };
        var parsed = new ParsedArmyList(
            Name: "Test Army", PointsSpent: 0, Faction: faction, Detachments: [detachment], ForceDisposition: "Test",
            BattleSize: "Incursion", PointsLimit: 1000, AttachmentGroups: [],
            StandaloneUnits: [new ParsedUnit("Intercessor Squad", [new ParsedModelGroup("Intercessor", 5, [])], [])]);

        var source = new LocalDiskBsdataCatalogueSource(BundledBsDataRoot());
        var catalogue = ResolvedBsdataCatalogue.Build(source, BsdataFactionResolver.ResolveStartingFileName(faction, source.ListFileNames()));
        return (Unit)ArmyRosterEnricher.Enrich(parsed, catalogue).Units.Single();
    }

    [Fact]
    public void WithoutItsDetachment_AUnitDoesNotCarryTemplarVows()
    {
        IntercessorSquad("Fist of the God-Emperor").Datasheet.Abilities.Should().NotContain(a => a.Name == "Templar Vows");
    }

    [Fact]
    public void WithItsDetachment_AUnitCarriesTemplarVows()
    {
        IntercessorSquad("Vow-Sworn Crusaders").Datasheet.Abilities.Should().Contain(a => a.Name == "Templar Vows");
    }
}
