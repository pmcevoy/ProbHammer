using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataClosureResolverTests
{
    [Fact]
    public void Multi_level_transitive_import_includes_every_reachable_file()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        closure.Files.Select(f => f.FileName).Should().BeEquivalentTo(
            "closure-starting.json", "Imported Faction.json", "Deep Import.json");
    }

    [Fact]
    public void Starting_file_is_first_in_resolution_order()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        closure.Files[0].FileName.Should().Be("closure-starting.json");
    }

    [Fact]
    public void A_stale_cached_link_name_still_resolves_via_the_targets_own_catalogue_id()
    {
        // Mirrors a real-data finding: Chaos - Chaos Space Marines.json's catalogueLinks entry
        // named "Chaos - Daemons Library" whose actual target file is
        // "Chaos - Chaos Daemons Library.json" - the cached link name had drifted from the
        // target's current file name. Only targetId (matched against each candidate file's own
        // top-level catalogue id) reliably identifies the right file.
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-mismatch-starting.json");

        closure.Files.Select(f => f.FileName).Should().Contain("closure-mismatch-actual-name.json");
    }
}
