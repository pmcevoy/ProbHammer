using System.Text.RegularExpressions;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

public sealed record ResolvedName(string Kind, string Verbatim, string Canonical);

/// <summary>Collect-time resolution of every BSData-vocabulary name in one classification. An
/// unresolved name demotes a <c>complete</c> record - see
/// <see cref="ClassificationRecord.EffectiveCoverageStatus"/>.</summary>
public sealed record NameResolution(IReadOnlyList<ResolvedName> Resolved, IReadOnlyList<string> Unresolved);

/// <summary>Resolves the names the model records verbatim - weapon keywords, granted ability names,
/// named-weapon selectors, target keywords - against the corpus vocabulary, ignoring case, markup and
/// punctuation. Each match's canonical form is the corpus's most common spelling.</summary>
public sealed partial class NameResolver
{
    private readonly IReadOnlyDictionary<string, string> _weaponKeywords;
    private readonly IReadOnlyDictionary<string, string> _weaponNames;
    private readonly IReadOnlyDictionary<string, string> _unitKeywords;
    private readonly IReadOnlyDictionary<string, string> _abilityNames;

    public NameResolver(CorpusVocabulary vocabulary)
    {
        _weaponKeywords = IndexByKey(vocabulary.WeaponKeywords);
        _weaponNames = IndexByKey(vocabulary.WeaponNames);
        _unitKeywords = IndexByKey(vocabulary.UnitKeywords);
        _abilityNames = IndexByKey(vocabulary.AbilityNames);
    }

    public NameResolution Resolve(ClassificationResult classification)
    {
        var resolved = new List<ResolvedName>();
        var unresolved = new List<string>();

        void Record(string kind, string verbatim, string? canonical)
        {
            if (canonical is null)
                unresolved.Add($"{kind} '{verbatim}'");
            else
                resolved.Add(new ResolvedName(kind, verbatim, canonical));
        }

        if (classification.Target is KeywordTarget keywordTarget)
            foreach (var keyword in keywordTarget.Keywords ?? [])
                Record("UnitKeyword", keyword, Lookup(_unitKeywords, keyword));

        foreach (var classified in classification.Effects)
        {
            switch (classified.Effect)
            {
                case WeaponKeywordGrantEffect grant:
                    ResolveSelector(grant.Selector, Record);
                    Record("WeaponKeyword", grant.Keyword, Lookup(_weaponKeywords, grant.Keyword));
                    if (grant.ReplacesKeyword is { } replaced)
                        Record("WeaponKeyword", replaced, Lookup(_weaponKeywords, replaced));
                    break;
                case WeaponCharacteristicEffect characteristic:
                    ResolveSelector(characteristic.Selector, Record);
                    break;
                case NamedAbilityGrantEffect abilityGrant:
                    Record("AbilityName", abilityGrant.AbilityName, LookupAbility(abilityGrant.AbilityName));
                    break;
            }
        }

        return new NameResolution(resolved, unresolved);
    }

    private void ResolveSelector(WeaponSelector selector, Action<string, string, string?> record)
    {
        if (selector is NamedWeaponSelector named)
            record("WeaponName", named.Name, LookupWeaponName(named.Name));
    }

    private string? LookupWeaponName(string name)
    {
        var key = Key(name);
        foreach (var candidate in new[] { key, TrimSuffix(key, "es"), TrimSuffix(key, "s") })
            if (candidate is not null && _weaponNames.TryGetValue(candidate, out var canonical))
                return canonical;
        return null;
    }

    // A granted ability usually carries a value BSData doesn't list verbatim (Scouts 9" vs. the rule
    // "Scouts"), so fall back to the base name and keep the model's own value.
    private string? LookupAbility(string name)
    {
        if (Lookup(_abilityNames, name) is { } exact)
            return exact;

        var match = TrailingValuePattern().Match(name.Trim());
        return match.Success && Lookup(_abilityNames, name.Trim()[..match.Index]) is { } baseName
            ? $"{baseName} {match.Groups["value"].Value}"
            : null;
    }

    private static string? Lookup(IReadOnlyDictionary<string, string> index, string name) =>
        index.TryGetValue(Key(name), out var canonical) ? canonical : null;

    private static string? TrimSuffix(string key, string suffix) =>
        key.Length > suffix.Length && key.EndsWith(suffix, StringComparison.Ordinal) ? key[..^suffix.Length] : null;

    private static IReadOnlyDictionary<string, string> IndexByKey(IReadOnlyDictionary<string, int> spellings) =>
        spellings
            .GroupBy(kvp => Key(kvp.Key))
            .Where(group => group.Key.Length > 0)
            .ToDictionary(group => group.Key,
                group => group.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
                    .First().Key);

    private static string Key(string text) => NonKeyCharacters().Replace(text.ToLowerInvariant(), "");

    [GeneratedRegex("[^a-z0-9+\"]")]
    private static partial Regex NonKeyCharacters();

    [GeneratedRegex(@"\s+(?<value>(?:d\d+(?:\+\d+)?|\d+)(?:\+|"")?)$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingValuePattern();
}
