using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Classifies a caveated InSv's linked Ability text against a fixed, closed set of known
/// templates (see invulnerable-save's "Footnoted Caveat Text Resolution"), shared by both import
/// pipelines so a footnote whose ability text is one of these exact sentences resolves to a real
/// melee/ranged split instead of staying caveated. Anchored, whole-string matches only - no
/// substring/fuzzy matching, so extra leading/trailing text never resolves.</summary>
public static partial class InvulnerableSaveCaveatClassifier
{
    [GeneratedRegex(@"^This model has a (\d+)\+ invulnerable save against ranged attacks\.$")]
    private static partial Regex ModelRangedTemplate();

    [GeneratedRegex(@"^This model has a (\d+)\+ invulnerable save against melee attacks\.$")]
    private static partial Regex ModelMeleeTemplate();

    [GeneratedRegex(@"^Models in this unit have a (\d+)\+ invulnerable save against ranged attacks\.$")]
    private static partial Regex UnitRangedTemplate();

    [GeneratedRegex(@"^Models in this unit have a (\d+)\+ invulnerable save against melee attacks\.$")]
    private static partial Regex UnitMeleeTemplate();

    private static (int Value, bool IsRanged)? MatchTemplate(string abilityText)
    {
        // A real BSData authoring quirk (Space Marines' Judiciar) uses a U+00A0 no-break space in
        // place of one plain space mid-template - the same template, not a different one, so it's
        // normalized away here rather than left to silently miss resolution.
        var normalized = abilityText.Replace(' ', ' ');

        foreach (var (regex, isRanged) in new[]
                 {
                     (ModelRangedTemplate(), true), (ModelMeleeTemplate(), false),
                     (UnitRangedTemplate(), true), (UnitMeleeTemplate(), false)
                 })
        {
            var match = regex.Match(normalized);
            if (match.Success)
                return (int.Parse(match.Groups[1].Value), isRanged);
        }

        return null;
    }

    /// <summary>A single footnoted value (e.g. "5+*"): resolves to the template's value on the
    /// matched attack type, 0 on the other, with no digit-consistency check against the raw text -
    /// the footnote carries no digit of its own to compare against.</summary>
    public static (int Melee, int Ranged)? TryResolveBare(string abilityText)
    {
        var match = MatchTemplate(abilityText);
        return match is { } m ? (m.IsRanged ? 0 : m.Value, m.IsRanged ? m.Value : 0) : null;
    }

    /// <summary>A melee/ranged pair with exactly one footnoted side: resolves the footnoted side to
    /// the matched template's value and keeps the pair's other, already-known plain digit - but only
    /// when the template's own value agrees with <paramref name="footnotedDigit"/> (the digit already
    /// present in the raw footnoted text); a mismatch leaves the result caveated rather than trusting
    /// either value.</summary>
    public static (int Melee, int Ranged)? TryResolveSplit(string abilityText, int footnotedDigit, int plainDigit)
    {
        var match = MatchTemplate(abilityText);
        if (match is not { } m || m.Value != footnotedDigit) return null;
        return m.IsRanged ? (plainDigit, m.Value) : (m.Value, plainDigit);
    }
}