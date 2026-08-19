using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataNameResolverTests
{
    [Fact]
    public void Locally_defined_entry_wins_over_a_same_named_import()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        var resolved = BsdataNameResolver.Resolve(closure, "Locally Defined Unit");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("s1"); // the starting file's own entry, not "Imported Faction"'s i1
    }

    [Fact]
    public void Name_absent_locally_falls_through_to_an_imported_catalogue()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        var resolved = BsdataNameResolver.Resolve(closure, "Import Only Unit");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("i2");
    }

    [Fact]
    public void Falls_through_multiple_import_levels()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        var resolved = BsdataNameResolver.Resolve(closure, "Deep Unit");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("d1");
    }

    [Fact]
    public void Unresolvable_name_returns_null()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "closure-starting.json");

        BsdataNameResolver.Resolve(closure, "Nonexistent Unit").Should().BeNull();
    }
}
