using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// A single normalized-key index of every <see cref="RuleDefinition"/> reachable from one
/// <see cref="BsdataClosure"/> - each catalogue file's own `rules` array (faction/library rule
/// text) plus the game system's `sharedRules` array (universal rule text). Built once alongside
/// <see cref="ResolvedBsdataCatalogue"/> and cached at the same tier - see design.md's "Glossary
/// is built alongside ResolvedBsdataCatalogue, cached at the same tier".
/// </summary>
public sealed partial class RuleGlossary
{
    private readonly IReadOnlyDictionary<string, RuleDefinition> _byNormalizedKey;

    private RuleGlossary(IReadOnlyDictionary<string, RuleDefinition> byNormalizedKey)
    {
        _byNormalizedKey = byNormalizedKey;
    }

    /// <summary>Iterates <paramref name="closure"/>.Files in their existing resolution order
    /// (starting file nearest, imports farther) collecting each file's own `Rules`, then
    /// `closure.GameSystem?.SharedRules` for the universal set. Indexes each rule by its own
    /// <see cref="Normalize"/>d `Name` *and* every <see cref="Normalize"/>d `Alias` - for most
    /// real rules these collapse to the same key (Name and Alias are almost always the same
    /// phrase, just cased differently: "Sustained Hits" / "SUSTAINED HITS"), but indexing both
    /// means a rule with no `Alias` at all (confirmed real BSData data gaps: "Cleave",
    /// "Close-quarters" - each self-references itself in its own description text via the same
    /// bracket convention every other generic mechanic uses, e.g. Cleave's own text says
    /// "**[CLEAVE X]**") still resolves via its Name, rather than requiring a formally-declared
    /// alias that BSData simply never populated. First occurrence wins per key, mirroring
    /// <see cref="BsdataNameResolver"/>'s existing "local beats imported" and
    /// <see cref="BsdataDatasheetMapper"/>'s existing "first occurrence wins" dedup conventions.
    /// </summary>
    public static RuleGlossary Build(BsdataClosure closure)
    {
        var index = new Dictionary<string, RuleDefinition>(StringComparer.Ordinal);

        void AddAll(IEnumerable<BsRule> rules)
        {
            foreach (var rule in rules)
            {
                var definition = new RuleDefinition(rule.Name, rule.Alias, rule.Description, rule.Modifiers);
                index.TryAdd(Normalize(rule.Name), definition);
                foreach (var alias in rule.Alias)
                    index.TryAdd(Normalize(alias), definition);
            }
        }

        foreach (var (_, catalogue) in closure.Files)
            AddAll(catalogue.Rules);

        if (closure.GameSystem is not null)
            AddAll(closure.GameSystem.SharedRules);

        return new RuleGlossary(index);
    }

    /// <summary>Resolves a normalized bracket token (per <see cref="RuleTextTokenizer"/>) or a
    /// literal weapon-keyword tag (per `WeaponAbilityTags()`) by comparing its own
    /// <see cref="Normalize"/>d form against every rule's Name/Alias, normalized the same way -
    /// mirroring <see cref="Datasheet.TryGetStatline"/>/<see cref="Datasheet.TryResolveWeaponProfile"/>'s
    /// existing non-throwing-lookup convention.</summary>
    public RuleDefinition? TryResolve(string nameOrAlias) =>
        _byNormalizedKey.TryGetValue(Normalize(nameOrAlias), out var definition) ? definition : null;

    // A generic mechanic's Name/Alias appears bare in the glossary (e.g. "Sustained Hits"/
    // "SUSTAINED HITS", "Anti"/"ANTI"), but real ability/weapon text always appends a value or
    // (for Anti only) a target category to it (e.g. "SUSTAINED HITS 1", "ANTI-VEHICLE 3+") -
    // confirmed by Sustained Hits' and Anti's own rule text, which each document their referenced
    // form explicitly ("**[SUSTAINED HITS X]**", "**[ANTI-X Y+]**"). Normalize applies a small,
    // ordered pipeline so both sides of a comparison land on the same key regardless of that
    // appended value, casing, or punctuation. Order matters at every step:
    //   1. Strip a trailing value/threshold/dice/placeholder suffix (a bare "x" placeholder, a
    //      digit run, a digit run with a trailing "+", or "d" + digits with an optional
    //      "+"-suffix, e.g. "d3") - must run first, while the whitespace separator it matches on
    //      is still present.
    //   2. Collapse anything starting with "anti" followed by a space/hyphen/colon boundary to
    //      bare "anti" - Anti's real referenced shape is two-part ("category" + "value", not just
    //      a trailing value: confirmed further by a real negated-target variant,
    //      "ANTI: non-MONSTER/VEHICLE 5+"), so trailing-suffix removal alone can't recover it. A
    //      blanket "cut at the first hyphen" rule was rejected as unsafe - "Twin-linked"'s hyphen
    //      is part of the rule's own name, not a category separator - so Anti gets this one named
    //      exception instead (see design.md). The boundary check matters: without it, an
    //      unrelated word merely starting with "anti" (e.g. "Antimatter") would wrongly collapse
    //      too - and it must run before step 3 removes that same boundary character, or there
    //      would be nothing left to check.
    //   3. Strip everything that isn't a letter or digit (spaces, hyphens, colons, slashes, "+")
    //      - this is also what makes a casing/hyphenation variant like "Twin-Linked" collapse to
    //      the same key as "TWIN-LINKED" with no separate casing rule needed.
    // Lowercasing happens first so every pattern below can use plain lowercase literals.
    private static string Normalize(string text)
    {
        var normalized = text.ToLowerInvariant();
        foreach (var (pattern, replacement) in NormalizationSteps)
            normalized = pattern().Replace(normalized, replacement);

        return normalized;
    }

    private static readonly (Func<Regex> Pattern, string Replacement)[] NormalizationSteps =
    [
        (TrailingValueSuffixPattern, ""),
        (AntiPrefixPattern, "anti"),
        (PunctuationAndWhitespacePattern, ""),
    ];

    [GeneratedRegex(@"\s+(?:d\d+(?:\+\d+)?|\d+\+?|x)$")]
    private static partial Regex TrailingValueSuffixPattern();

    [GeneratedRegex(@"^anti[\s\-:].*")]
    private static partial Regex AntiPrefixPattern();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex PunctuationAndWhitespacePattern();
}
