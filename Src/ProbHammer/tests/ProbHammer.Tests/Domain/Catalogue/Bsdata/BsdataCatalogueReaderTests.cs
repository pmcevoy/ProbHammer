using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataCatalogueReaderTests
{
    [Fact]
    public void Reads_a_real_sample_file_end_to_end()
    {
        var json = BsdataFixtures.Source().GetJson("imperial-fists-single-model.json");

        var catalogue = BsdataCatalogueReader.Read(json);

        catalogue.SharedSelectionEntries.Should().ContainSingle(e => e.Name == "Darnath Lysander");
    }

    [Fact]
    public void Deserializes_a_file_carrying_constraints_modifiers_and_associations_without_error()
    {
        // Imperium - Black Templars.json (source of this fixture) carries constraints, modifiers,
        // conditionGroups, and associations throughout - none of them are modeled by BsSelectionEntry,
        // and System.Text.Json silently ignores unmapped JSON properties rather than failing.
        var json = BsdataFixtures.Source().GetJson("black-templars-crusader-squad.json");

        var catalogue = BsdataCatalogueReader.Read(json);

        catalogue.SharedSelectionEntries.Should().ContainSingle(e => e.Name == "Crusader Squad");
    }

    [Fact]
    public void Deserializes_a_game_system_files_shared_rules()
    {
        var json = BsdataFixtures.Source().GetJson("game-system-shared-rules.json");

        var catalogue = BsdataCatalogueReader.Read(json);

        var lethalHits = catalogue.SharedRules.Should().ContainSingle(r => r.Name == "Lethal Hits").Subject;
        lethalHits.Alias.Should().ContainSingle("LETHAL HITS");
        lethalHits.Description.Should().Contain("automatically wound the target");
    }

    [Fact]
    public void Deserializes_a_faction_or_library_files_rules()
    {
        var json = BsdataFixtures.Source().GetJson("library-faction-rules.json");

        var catalogue = BsdataCatalogueReader.Read(json);

        var templarVows = catalogue.Rules.Should().ContainSingle(r => r.Name == "Templar Vows").Subject;
        templarVows.Description.Should().Contain("Army Faction");
        templarVows.Alias.Should().BeEmpty();
    }

    [Fact]
    public void A_file_with_neither_rules_key_present_still_deserializes_cleanly()
    {
        var json = BsdataFixtures.Source().GetJson("closure-starting.json");

        var catalogue = BsdataCatalogueReader.Read(json);

        catalogue.Rules.Should().BeEmpty();
        catalogue.SharedRules.Should().BeEmpty();
    }
}
