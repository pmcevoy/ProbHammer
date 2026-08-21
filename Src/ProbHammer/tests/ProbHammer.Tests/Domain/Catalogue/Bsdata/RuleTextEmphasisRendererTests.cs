using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Exercises <see cref="RuleTextEmphasisRenderer"/> in isolation from bracket resolution -
/// every test passes a trivial pass-through bracket delegate (renders the bracket's raw text
/// unchanged, as if never resolved), per task 6.3a's "unit tests for the emphasis renderer".</summary>
public class RuleTextEmphasisRendererTests
{
    private static (string Inline, string Popovers) Render(string text) =>
        RuleTextEmphasisRenderer.Render(text, raw => (raw, ""));

    [Fact]
    public void Single_asterisk_renders_italic()
    {
        var (inline, popovers) = Render("*italic text*");

        inline.Should().Be("<i>italic text</i>");
        popovers.Should().BeEmpty();
    }

    [Fact]
    public void Double_asterisk_renders_bold()
    {
        var (inline, _) = Render("**bold text**");

        inline.Should().Be("<b>bold text</b>");
    }

    [Fact]
    public void Double_caret_renders_small_caps()
    {
        var (inline, _) = Render("^^small caps text^^");

        inline.Should().Be("<span class=\"rule-text-smallcaps\">small caps text</span>");
    }

    [Fact]
    public void Bold_and_small_caps_combine_when_both_wrap_the_same_text()
    {
        var (inline, _) = Render("^^**Fabius Bile**^^");

        inline.Should().Be("<span class=\"rule-text-smallcaps\"><b>Fabius Bile</b></span>");
    }

    [Fact]
    public void Nested_triple_asterisk_emphasis_renders_as_combined_bold_and_italic()
    {
        const string text =
            "***Designer's Note:** Choosing to automatically wound the target means that no " +
            "**wound roll** is made for that attack. You may decide against this, as it means " +
            "that attack cannot result in a **critical wound** and so cannot trigger other " +
            "abilities such as [DEVASTATING WOUNDS].*";

        var (inline, _) = Render(text);

        // WebUtility.HtmlEncode escapes the apostrophe in "Designer's" to &#39; - correct, expected
        // encoding, not a rendering bug.
        inline.Should().StartWith("<b><i>Designer&#39;s Note:</i></b>");
        inline.Should().Contain("<i> Choosing to automatically wound the target means that no </i>");
        inline.Should().Contain("<b><i>wound roll</i></b>");
        inline.Should().Contain("<i> is made for that attack. You may decide against this, as it means " +
                                 "that attack cannot result in a </i>");
        inline.Should().Contain("<b><i>critical wound</i></b>");
        // The bracket reference gets its own adjacent <i> wrapper (via AppendStyled at the point
        // the bracket delegate result is spliced in) rather than being merged into the surrounding
        // text's own <i> run - functionally identical once rendered (adjacent same-tag elements
        // read as one continuous italic span), just not string-merged.
        inline.Should().EndWith("<i> and so cannot trigger other abilities such as </i><i>DEVASTATING WOUNDS</i><i>.</i>");
    }

    [Fact]
    public void Plain_text_with_no_markup_passes_through_unchanged()
    {
        var (inline, _) = Render("No emphasis here.");

        inline.Should().Be("No emphasis here.");
    }

    [Fact]
    public void Text_is_html_encoded()
    {
        var (inline, _) = Render("A < B & C > D");

        inline.Should().Be("A &lt; B &amp; C &gt; D");
    }

    [Fact]
    public void A_bracket_token_stays_extractable_and_reaches_the_delegate_inside_open_emphasis()
    {
        var seen = new List<string>();
        var (inline, _) = RuleTextEmphasisRenderer.Render(
            "*text with [A REFERENCE] inside*",
            raw => { seen.Add(raw); return (raw, ""); });

        seen.Should().ContainSingle("A REFERENCE");
        // The bracket's own delegate result gets wrapped in the currently-open italic span too
        // (its own adjacent <i>, not merged with the surrounding text's) - the bracket stayed
        // reachable through the open emphasis exactly as design.md's confirmed real shape requires.
        inline.Should().Be("<i>text with </i><i>A REFERENCE</i><i> inside</i>");
    }

    [Fact]
    public void Popovers_from_the_bracket_delegate_are_returned_separately_from_inline_html()
    {
        var (inline, popovers) = RuleTextEmphasisRenderer.Render(
            "Grants [LETHAL HITS].",
            raw => ("<button>LETHAL HITS</button>", "<div class=\"rule-popover\">text</div>"));

        inline.Should().Be("Grants <button>LETHAL HITS</button>.");
        popovers.Should().Be("<div class=\"rule-popover\">text</div>");
    }
}
