using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Exercises `RuleTextTokenizer.ExtractBracketTokens` + `RuleGlossary.TryResolve`
/// together - resolution is exact-match only, per rules-glossary's "Bracket Token Resolution Is
/// Exact-Match Only".</summary>
public class BracketTokenResolutionTests
{
    [Fact]
    public void An_extracted_token_with_an_exact_glossary_match_resolves_to_the_right_definition()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json");
        var glossary = RuleGlossary.Build(closure);

        var tokens = RuleTextTokenizer.ExtractBracketTokens("Grants the **[LETHAL HITS]** ability.");

        var resolved = tokens.Select(glossary.TryResolve).Single();
        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Lethal Hits");
    }

    [Fact]
    public void An_unmatched_token_resolves_to_null_without_affecting_the_rest_of_the_text()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json");
        var glossary = RuleGlossary.Build(closure);

        var tokens = RuleTextTokenizer.ExtractBracketTokens("Grants [MADE UP RULE] and **[LETHAL HITS]**.");

        var resolved = tokens.Select(glossary.TryResolve).ToList();
        resolved[0].Should().BeNull();
        resolved[1].Should().NotBeNull();
        resolved[1]!.Name.Should().Be("Lethal Hits");
    }
}
