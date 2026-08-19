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
}
