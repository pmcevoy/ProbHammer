using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Extracts `[BRACKET]`-delimited cross-reference tokens from a piece of ability/rule text - a
/// pure, `RuleGlossary`-independent step (see design.md's "Bracket-token extraction is a separate,
/// pure function from resolution"). Deliberately has no awareness of `*`/`**`/`^^` emphasis markup
/// as a *signal* of whether something is a candidate reference at all - that's what makes
/// "Non-Bracket Markup Is Not A Reference Signal" hold structurally rather than needing to
/// explicitly exclude those characters. It does, however, normalize each already-captured token's
/// own text before returning it (design.md's "Bracket-token extraction is a separate, pure
/// function from resolution"): `*`/`^` characters found *inside* the brackets are stripped
/// (confirmed real shape: `[^^Lethal Hits^^]`, a rule referenced by its display Name wrapped in
/// small-caps markup inside the brackets themselves - the reverse of markup wrapping a bracket
/// from the outside), and known Unicode whitespace/hyphen variants are canonicalized to their
/// plain-ASCII equivalent (confirmed real shapes: a U+00A0 no-break space inside
/// "DEVASTATING[nbsp]WOUNDS"; a U+2011 non-breaking hyphen inside several "ANTI-VEHICLE"-style
/// references; a U+2013 en dash, not yet confirmed but the same family of authoring noise). This
/// stays glossary-independent - it corrects encoding-level noise, never interprets or discards a
/// token's semantic content (e.g. a value or category token is left untouched here; see
/// `RuleGlossary`'s generic-mechanic fallback for where that's handled).
/// </summary>
public static partial class RuleTextTokenizer
{
    // Simple non-nested "[...]" extraction - no real example of a nested "[[...]]" token has been
    // observed in the corpus (see design.md).
    [GeneratedRegex(@"\[([^\[\]]+)\]")]
    private static partial Regex BracketTokenPattern();

    public static IReadOnlyList<string> ExtractBracketTokens(string text) =>
        BracketTokenPattern().Matches(text).Select(m => Normalize(m.Groups[1].Value)).ToList();

    /// <summary>Exposed for reuse by any other caller that has already captured a bracket's raw
    /// inner text by some other means (e.g. `RuleTextEmphasisRenderer`, which needs the same
    /// light normalization applied to a bracket token encountered mid-parse, before resolving it
    /// against a `RuleGlossary`) and just needs this one cleanup step, not full extraction.</summary>
    public static string Normalize(string token) =>
        token
            .Replace("^", "")
            .Replace("*", "")
            .Replace(' ', ' ')  // no-break space -> plain space
            .Replace('‑', '-') // non-breaking hyphen -> plain hyphen
            .Replace('–', '-'); // en dash -> plain hyphen
}
