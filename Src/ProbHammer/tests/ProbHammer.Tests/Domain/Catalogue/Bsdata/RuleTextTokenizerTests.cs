using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class RuleTextTokenizerTests
{
    [Fact]
    public void A_bracketed_token_embedded_in_running_text_extracts_cleanly()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("...has the **[PRECISION]** ability...");

        tokens.Should().ContainSingle("PRECISION");
    }

    [Fact]
    public void Small_caps_and_bold_wrapped_text_produces_no_tokens()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("^^**Fabius Bile**^^ leads ^^**Warlord**^^ into battle.");

        tokens.Should().BeEmpty();
    }

    [Fact]
    public void Inconsistently_nested_markup_does_not_affect_extraction()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("^^Fabius Bile^^** joins the [RETINUE].");

        tokens.Should().ContainSingle("RETINUE");
    }

    [Fact]
    public void A_bracket_token_stays_extractable_inside_deeply_nested_emphasis()
    {
        const string text =
            "***Designer's Note:** Choosing to automatically wound the target means that no " +
            "**wound roll** is made for that attack. You may decide against this, as it means " +
            "that attack cannot result in a **critical wound** and so cannot trigger other " +
            "abilities such as [DEVASTATING WOUNDS].*";

        var tokens = RuleTextTokenizer.ExtractBracketTokens(text);

        tokens.Should().ContainSingle("DEVASTATING WOUNDS");
    }

    [Fact]
    public void Multiple_bracket_tokens_all_extract()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("Grants [LETHAL HITS] and [SUSTAINED HITS 1].");

        tokens.Should().Equal("LETHAL HITS", "SUSTAINED HITS 1");
    }

    [Fact]
    public void Text_with_no_brackets_extracts_nothing()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("Plain text with no cross-references.");

        tokens.Should().BeEmpty();
    }

    [Fact]
    public void Markup_characters_inside_the_bracket_itself_are_stripped()
    {
        var tokens = RuleTextTokenizer.ExtractBracketTokens("Has the [^^Lethal Hits^^] ability.");

        tokens.Should().ContainSingle("Lethal Hits");
    }

    [Fact]
    public void A_non_breaking_space_inside_a_token_normalizes_to_a_plain_space()
    {
        var text = "Has the [DEVASTATING WOUNDS] ability.";

        var tokens = RuleTextTokenizer.ExtractBracketTokens(text);

        tokens.Should().ContainSingle("DEVASTATING WOUNDS");
    }

    [Fact]
    public void A_non_breaking_hyphen_inside_a_token_normalizes_to_a_plain_hyphen()
    {
        var text = "Has the [ANTI‑VEHICLE 3+] ability.";

        var tokens = RuleTextTokenizer.ExtractBracketTokens(text);

        tokens.Should().ContainSingle("ANTI-VEHICLE 3+");
    }

    [Fact]
    public void An_en_dash_inside_a_token_normalizes_to_a_plain_hyphen()
    {
        var text = "Has the [ANTI–VEHICLE 3+] ability.";

        var tokens = RuleTextTokenizer.ExtractBracketTokens(text);

        tokens.Should().ContainSingle("ANTI-VEHICLE 3+");
    }
}
