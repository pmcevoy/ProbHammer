using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Resolves one parsed Detachments entry's captured text (`army-list-parsing`'s Army Metadata
/// Extraction - a single string that MAY itself name more than one Detachment in natural-language
/// list form, e.g. "Fulguris Task Force, Marshal's Household, and Subversion Assets") against a
/// faction's <see cref="ResolvedBsdataCatalogue"/> via a greedy, longest-known-name-first "chomp" -
/// never a syntactic split on "and"/commas, since a real Detachment can itself be named with the
/// word "and" in it ("Legends of Saga and Song") and one Detachment's name can be a literal
/// substring of another's ("Warhost"/"Armoured Warhost") - see design.md's algorithm and
/// army-roster-enrichment's Detachment Name Resolution requirement.
/// </summary>
public static partial class DetachmentNameResolver
{
    public static IReadOnlyList<ResolvedDetachment> Resolve(string text, ResolvedBsdataCatalogue catalogue)
    {
        var chars = text.ToCharArray();
        var claims = new List<(int Index, string Name)>();

        // Longest names first: once a longer name's own span is blanked out, a shorter name that
        // was only ever a substring of it (e.g. "Warhost" inside "Armoured Warhost") has nothing
        // left to match against - no separate containment-filtering pass needed.
        foreach (var name in catalogue.DetachmentNames.OrderByDescending(n => n.Length))
        {
            var index = new string(chars).IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            claims.Add((index, name));
            for (var i = index; i < index + name.Length; i++)
                chars[i] = ' ';
        }

        var remainder = StripSeparators(new string(chars));
        if (remainder.Length > 0)
        {
            var suggestion = BsdataNameSuggestion.FindClosest(remainder, catalogue.DetachmentNames);
            var message = suggestion is null
                ? $"Could not resolve Detachment text '{remainder}'."
                : $"Could not resolve Detachment text '{remainder}'. Did you mean '{suggestion}'?";
            throw new BsdataNameResolutionException(remainder, message);
        }

        // Claims are recorded against stable positions in the original text (matched spans are
        // blanked, never spliced out), so ordering by Index recovers the order each Detachment was
        // actually listed in - not the order the longest-first matching pass happened to find them.
        return claims
            .OrderBy(c => c.Index)
            .Select(c => BuildResolvedDetachment(catalogue.ResolveDetachment(c.Name), catalogue.Glossary))
            .ToList();
    }

    private static ResolvedDetachment BuildResolvedDetachment(BsSelectionEntry entry, RuleGlossary glossary) =>
        new(entry.Name, DetachmentRuleTextExtractor.Extract(entry, glossary)
            .Select(pair => new DetachmentRule(pair.Name, pair.Text))
            .ToList());

    // Everything left once every known Detachment name has been blanked out should be nothing but
    // separator punctuation (a comma, the word "and", or whitespace) - anything else is a genuine
    // unresolved remainder (a typo, or a Detachment this closure doesn't know about).
    private static string StripSeparators(string text)
    {
        var noCommas = text.Replace(",", " ");
        var noAnd = AndWordPattern().Replace(noCommas, " ");
        return CollapseWhitespacePattern().Replace(noAnd, " ").Trim();
    }

    [GeneratedRegex(@"\band\b", RegexOptions.IgnoreCase)]
    private static partial Regex AndWordPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespacePattern();
}