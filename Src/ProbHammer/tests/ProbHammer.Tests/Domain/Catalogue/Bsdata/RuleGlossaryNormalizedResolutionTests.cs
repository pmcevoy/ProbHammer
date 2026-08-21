using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Exercises <see cref="RuleGlossary"/>'s normalized-key resolution - see design.md's
/// "Bounded normalization before resolution, not fuzzy matching" and rules-glossary's
/// "Bracket Token Resolution Is Exact-Match, With A Bounded Generic-Mechanic Fallback".</summary>
public class RuleGlossaryNormalizedResolutionTests
{
    private static RuleGlossary BuildGlossary() =>
        RuleGlossary.Build(BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "library-faction-rules.json"));

    [Fact]
    public void A_value_suffixed_token_resolves()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("SUSTAINED HITS 1");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Sustained Hits");
    }

    [Fact]
    public void A_dice_valued_token_resolves()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("SUSTAINED HITS D3");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Sustained Hits");
    }

    [Fact]
    public void A_placeholder_x_token_resolves()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("SUSTAINED HITS X");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Sustained Hits");
    }

    [Fact]
    public void A_title_case_value_suffixed_token_also_resolves()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("Sustained Hits D3");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Sustained Hits");
    }

    [Fact]
    public void A_target_category_and_value_suffixed_anti_token_resolves_via_the_named_exception()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("ANTI-VEHICLE 3+");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Anti");
    }

    [Fact]
    public void A_negated_anti_target_resolves_via_the_named_exception()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("ANTI: non-MONSTER/VEHICLE 5+");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Anti");
    }

    [Fact]
    public void A_title_case_hyphenated_name_resolves_despite_the_casing_difference()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("Twin-Linked");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Twin-linked");
    }

    [Fact]
    public void A_value_suffixed_token_for_a_rule_with_no_declared_alias_still_resolves_via_its_name()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("CLEAVE 1");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Cleave");
    }

    [Fact]
    public void A_placeholder_token_for_a_rule_with_no_declared_alias_still_resolves_via_its_name()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("CLEAVE X");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Cleave");
    }

    [Fact]
    public void A_bare_token_for_a_rule_with_no_declared_alias_still_resolves_via_its_name()
    {
        var glossary = BuildGlossary();

        var resolved = glossary.TryResolve("CLOSE-QUARTERS");

        resolved.Should().NotBeNull();
        resolved!.Name.Should().Be("Close-quarters");
    }

    [Fact]
    public void An_unrecognized_word_starting_with_anti_still_only_matches_a_real_anti_reference()
    {
        var glossary = BuildGlossary();

        glossary.TryResolve("Antimatter Drive").Should().BeNull();
    }
}
