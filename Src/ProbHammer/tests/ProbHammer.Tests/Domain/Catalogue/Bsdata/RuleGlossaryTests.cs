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

    [Fact]
    public void WithFallback_prefers_the_receivers_definition_when_both_define_a_name()
    {
        var primary = RuleGlossary.BuildFrom([new RuleDefinition("Lethal Hits", [], "Roster text", [])]);
        var fallback = RuleGlossary.BuildFrom([new RuleDefinition("Lethal Hits", ["LETHAL HITS"], "Core text", [])]);

        var resolved = primary.WithFallback(fallback).TryResolve("LETHAL HITS");

        resolved!.Text.Should().Be("Roster text");
    }

    [Fact]
    public void WithFallback_resolves_a_name_only_the_fallback_defines()
    {
        var primary = RuleGlossary.BuildFrom([new RuleDefinition("Waaagh!", [], "Roster text", [])]);
        var fallback = RuleGlossary.BuildFrom([new RuleDefinition("Cleave", [], "Core text", [])]);

        var combined = primary.WithFallback(fallback);

        combined.TryResolve("CLEAVE 1")!.Text.Should().Be("Core text");
        combined.TryResolve("Waaagh!")!.Text.Should().Be("Roster text");
    }
}
