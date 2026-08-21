using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class RuleGlossaryTests
{
    [Fact]
    public void A_universal_rule_resolves_by_its_name()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json");
        var glossary = RuleGlossary.Build(closure);

        var resolved = glossary.TryResolve("Lethal Hits");

        resolved.Should().NotBeNull();
        resolved!.Text.Should().Contain("automatically wound the target");
    }

    [Fact]
    public void A_universal_rule_resolves_by_its_alias()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json");
        var glossary = RuleGlossary.Build(closure);

        var resolved = glossary.TryResolve("LETHAL HITS");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Lethal Hits");
    }

    [Fact]
    public void A_faction_or_library_rule_reached_transitively_through_the_closure_resolves()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "rule-glossary-transitive-import.json");
        var glossary = RuleGlossary.Build(closure);

        var resolved = glossary.TryResolve("Templar Vows");

        resolved.Should().NotBeNull();
        resolved!.Text.Should().Contain("Army Faction");
    }

    [Fact]
    public void An_unknown_name_returns_null_without_throwing()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json");
        var glossary = RuleGlossary.Build(closure);

        glossary.TryResolve("Nonexistent Rule").Should().BeNull();
    }

    [Fact]
    public void A_local_definition_wins_over_a_same_named_import()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "rule-glossary-collision-local.json");
        var glossary = RuleGlossary.Build(closure);

        var resolved = glossary.TryResolve("Duplicate Rule");

        resolved.Should().NotBeNull();
        resolved!.Text.Should().Contain("Local definition");
    }
}
