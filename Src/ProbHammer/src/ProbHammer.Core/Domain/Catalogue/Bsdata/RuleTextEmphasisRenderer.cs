using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Renders a piece of ability/rule text to HTML: `*italic*`/`**bold**`/`^^small-caps^^` markup
/// (including the nested `***bold+italic***` split-close case) becomes styling only - see
/// design.md's "Emphasis markup: a three-way mapping, not a link signal" - and every `[BRACKET]`
/// token encountered is handed to the caller-supplied <paramref name="renderBracket"/> delegate,
/// never resolved or interpreted here. A single left-to-right token scan (not sequential string
/// replacement - see design.md's rationale for why that would break the split-close case), so this
/// stays pure and independently testable with a trivial pass-through bracket delegate (task 6.3a's
/// tests use exactly that), while the real caller (`_UnitBlock.cshtml`) supplies a delegate that
/// resolves each bracket against a `RuleGlossary` and returns nested popover markup.
///
/// Every recursive call - rendering a bracket's own raw inner text (which may itself carry
/// emphasis markup, e.g. `[^^Lethal Hits^^]`), or a resolved reference's own `RuleDefinition.Text`
/// - returns its popover markup separately from its inline content, so a nested popover's `<div>`
/// never ends up wrapped inside an ancestor `<b>`/`<i>`/small-caps `<span>` (which would leak that
/// styling into the nested popover's own inherited CSS, since a popover's top-layer promotion
/// doesn't change its position in the DOM tree for inheritance purposes).
/// </summary>
public static partial class RuleTextEmphasisRenderer
{
    // Longest-first alternation so "***" doesn't accidentally match as "**" + "*" - order within
    // the alternation is what makes the greedy triple-marker case resolve correctly. The bracket
    // branch mirrors RuleTextTokenizer.BracketTokenPattern's own shape (simple, non-nested) - kept
    // as its own literal here rather than shared, since this scan needs it interleaved with the
    // emphasis markers in one single left-to-right pass, not resolved independently.
    [GeneratedRegex(@"\*\*\*|\*\*|\*|\^\^|\[[^\[\]]+\]")]
    private static partial Regex TokenPattern();

    /// <summary>Renders <paramref name="text"/>, returning its inline HTML (text and any bracket
    /// triggers, in place) separately from any popover panel markup those brackets produced (to be
    /// emitted as flow-content siblings, never nested inside this text's own emphasis wrapping).
    /// <paramref name="renderBracket"/> receives a bracket's raw, un-normalized inner text (the
    /// literal substring between `[` and `]`) and returns the same (Inline, Popovers) shape - so
    /// it can itself call back into this method for its own label/body text, recursively.</summary>
    public static (string Inline, string Popovers) Render(string text, Func<string, (string Inline, string Popovers)> renderBracket)
    {
        var inlineSb = new StringBuilder();
        var popoversSb = new StringBuilder();
        var bold = false;
        var italic = false;
        var smallCaps = false;
        var pos = 0;

        foreach (Match match in TokenPattern().Matches(text))
        {
            if (match.Index > pos)
                AppendText(inlineSb, text[pos..match.Index], bold, italic, smallCaps);

            var token = match.Value;
            if (token[0] == '[')
            {
                var (bracketInline, bracketPopovers) = renderBracket(token[1..^1]);
                AppendStyled(inlineSb, bracketInline, bold, italic, smallCaps);
                popoversSb.Append(bracketPopovers);
            }
            else
            {
                switch (token)
                {
                    case "***":
                        bold = !bold;
                        italic = !italic;
                        break;
                    case "**":
                        bold = !bold;
                        break;
                    case "*":
                        italic = !italic;
                        break;
                    case "^^":
                        smallCaps = !smallCaps;
                        break;
                }
            }

            pos = match.Index + match.Length;
        }

        if (pos < text.Length)
            AppendText(inlineSb, text[pos..], bold, italic, smallCaps);

        return (inlineSb.ToString(), popoversSb.ToString());
    }

    private static void AppendText(StringBuilder sb, string text, bool bold, bool italic, bool smallCaps)
    {
        if (text.Length == 0)
            return;

        AppendStyled(sb, WebUtility.HtmlEncode(text), bold, italic, smallCaps);
    }

    // Fixed nesting order (small-caps outermost, then bold, then italic) regardless of which
    // marker opened most recently - CSS-wise the visual result is identical either way (font-style/
    // font-weight/small-caps all apply independently), so a fixed order just keeps output
    // deterministic and testable. `html` is already-safe markup (either HTML-encoded plain text
    // from AppendText, or a bracket's own already-rendered result) - never re-encoded here.
    private static void AppendStyled(StringBuilder sb, string html, bool bold, bool italic, bool smallCaps)
    {
        if (html.Length == 0)
            return;

        if (smallCaps) sb.Append("<span class=\"rule-text-smallcaps\">");
        if (bold) sb.Append("<b>");
        if (italic) sb.Append("<i>");
        sb.Append(html);
        if (italic) sb.Append("</i>");
        if (bold) sb.Append("</b>");
        if (smallCaps) sb.Append("</span>");
    }
}
