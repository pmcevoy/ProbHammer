using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Classifies a rule/ability's Target and unconditional characteristic Effects from its own
/// Name+Text alone - no BSData JSON type, no `Domain.Catalogue.Bsdata` dependency, no roster
/// resolution. Anchored/template regex matching against known phrasings, same rigor as
/// <see cref="InvulnerableSaveCaveatClassifier"/> - not a general NLP/parsing approach. Unlike that
/// classifier's whole-string templates, these patterns search within arbitrary-length prose (a
/// Detachment rule/Core rule's own Text is rarely a single sentence), so the first pattern match wins;
/// see classify-rule-effects-from-text/design.md's Risks for why this is an accepted, fail-closed
/// trade-off rather than something this change tries to make smarter.</summary>
public static partial class RuleEffectClassifier
{
    private static readonly Dictionary<string, string> CharacteristicNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Movement"] = "M",
            ["Toughness"] = "T",
            ["Save"] = "Sv",
            ["Wounds"] = "W",
            ["Leadership"] = "Ld",
            ["Objective Control"] = "Oc"
        };

    /// <summary>Anchors an Effect pattern to the actual start of its own sentence - start-of-text, or
    /// immediately after a sentence-terminating PERIOD, with any following whitespace - so a match
    /// embedded inside an earlier comma-joined subordinate/conditional clause in the same sentence
    /// never succeeds (real corpus example: "If it does, until the end of the phase, the bearer has a
    /// 2+ invulnerable save." - "the bearer has..." is not at its sentence's true start). A structural
    /// signal, not a phrase denylist - it generalizes past "if it does"/"while X"/"each time X" and any
    /// other conditional-preamble phrasing without enumerating each one, the same way
    /// <see cref="InvulnerableSaveCaveatClassifier"/>'s own whole-string anchoring rules out extra
    /// leading text, just scoped to a sentence instead of the whole input (this classifier, unlike that
    /// one, must search within arbitrary-length, often multi-sentence prose - see the class's own doc
    /// comment). Confirmed real and previously a false positive on both Effect patterns before this
    /// anchor - caught live reviewing corpus-report output (2026-09-08), not by any test:
    /// invulnerable-save Relic grants gated behind "If it does, until the end of the phase, the bearer
    /// has a N+ invulnerable save." wrongly extracted a flat Set InSv, and Toughness/Objective Control
    /// buffs gated the same way wrongly extracted a flat Improve.
    ///
    /// Deliberately does NOT also accept a bare newline or bullet marker (■/▪) as a boundary - an
    /// earlier version of this anchor did, and that was ALSO a confirmed false positive, found by the
    /// same live review: a "select one of the following" menu (Moment Shackle's two bulleted
    /// alternatives; Combat Drugs'/Noospheric Transference's own numbered/headed option lists) uses a
    /// bullet or a bare newline-after-heading as its own item separator, which reads exactly like a
    /// fresh unconditional sentence start under a newline/bullet-permissive anchor but is actually one
    /// of several mutually-exclusive, player-selected alternatives - conditional on which option is
    /// chosen, same as any other conditional preamble. A period is the only boundary confirmed, across
    /// every real example checked, to mean "this is not itself part of a menu of alternatives."</summary>
    private const string SentenceStart = @"(?<=^|\.\s*)";

    // IgnoreCase on every pattern below EXCEPT AllCapsKeywordPhrase: real corpus text carries
    // inconsistent capitalization of the same plain-English phrasing (confirmed: "The bearer has a
    // 4+ Invulnerable save." alongside "...invulnerable save." for the exact same wargear item,
    // Storm Shield/Blizzard shield) - these are literal-word patterns, not signal-bearing case, so
    // case variance is noise to normalize past, the same class of harmless-variation problem
    // InvulnerableSaveCaveatClassifier's own NBSP normalization handles. AllCapsKeywordPhrase is the
    // one deliberate exception: there, capitalization IS the signal that distinguishes a real
    // keyword from an ordinary capitalized word, so making it case-insensitive would break it, not
    // fix it.

    /// <summary>"models in this unit" is functionally the same claim as "models in the bearer's
    /// unit" - an ability's own text describes its effect from the bearer's perspective, so "this
    /// unit" means the bearer's unit. Confirmed real and safe to recognize: surveyed ~30 real corpus
    /// occurrences of "models in this unit" and found zero where it meant anything other than the
    /// whole unit (never, e.g., a condition clause about some other, unrelated unit). Caught live
    /// reviewing corpus-report output (2026-09-08) - "Astartes Banner"'s "Add 1 to the Objective
    /// Control characteristic of models in this unit." was classified Self before this addition, only
    /// because it happens to use "this unit" instead of "the bearer's unit" for the identical
    /// claim.</summary>
    [GeneratedRegex("bearer's unit|models in this unit", RegexOptions.IgnoreCase)]
    private static partial Regex AttachedUnitPhrase();

    /// <summary>GW's own small-caps rules-text markup for a keyword (see
    /// <see cref="ProbHammer.Core.Domain.Catalogue.Bsdata.RuleTextEmphasisRenderer"/>'s
    /// "^^small-caps^^" handling), e.g. Templar Vows' "**^^Adeptus Astartes^^** units".</summary>
    [GeneratedRegex(@"\*\*\^\^([A-Za-z][A-Za-z '-]*)\^\^\*\*\s+units\b", RegexOptions.IgnoreCase)]
    private static partial Regex MarkupKeywordPhrase();

    /// <summary>A literal ALL-CAPS keyword run with no markup, e.g. Marshal's Household's "SWORD
    /// BRETHREN SQUAD units". Deliberately case-SENSITIVE - see the class-level note above.</summary>
    [GeneratedRegex(@"\b([A-Z]{2,}(?:\s[A-Z]{2,})*)\s+units\b")]
    private static partial Regex AllCapsKeywordPhrase();

    /// <summary>Requires a short (subject-shaped, comma-free) run of text between the sentence start
    /// (see <see cref="SentenceStart"/>) and "has" - "The bearer"/"This model"/"This unit"/a named
    /// character, never a subordinate clause. Also deliberately excludes a match immediately followed
    /// by "against" (e.g. "...invulnerable save against ranged attacks") via negative lookahead - that
    /// phrasing is an attack-type-restricted/split save (the same shape
    /// <see cref="InvulnerableSaveCaveatClassifier"/> models separately as a melee/ranged pair), which
    /// is conditional on the incoming attack's type, not the unconditional flat grant this pattern is
    /// meant to recognize. Confirmed real and previously a false positive: "This model has a 4+
    /// invulnerable save against ranged attacks..." (Ensorcelled Shield) and "The bearer has a 4+
    /// invulnerable save against ranged attacks, and a 5+ invulnerable save against melee attacks."
    /// (Veil of Medrengard) both wrongly extracted a flat Set InSv before this exclusion - caught live
    /// reviewing corpus-report output (2026-09-08), not by any test.</summary>
    [GeneratedRegex(SentenceStart + @"[A-Za-z][A-Za-z''\- ]{0,60} has an? (\d+)\+ invulnerable save(?!\s+against\b)",
        RegexOptions.IgnoreCase)]
    private static partial Regex InvulnerableSaveGrant();

    [GeneratedRegex(SentenceStart + @"Add (\d+) to the ([A-Za-z ]+?) characteristic", RegexOptions.IgnoreCase)]
    private static partial Regex AddCharacteristic();

    public static RuleClassification Classify(string name, string text)
    {
        var normalized = Normalize(text);
        return new RuleClassification(ClassifyTarget(normalized), ClassifyEffects(normalized));
    }

    /// <summary>Normalizes known-harmless real-corpus authoring variance before matching: a
    /// typographic U+2019 apostrophe (confirmed: Adeptus Custodes' Vexilla has both variants across
    /// different entries) and a U+00A0 non-breaking space used in place of a plain space (confirmed
    /// widespread - 1,543 of ~7,660 raw ability texts in the live corpus contain at least one),
    /// mirroring <see cref="InvulnerableSaveCaveatClassifier"/>'s own identical NBSP normalization -
    /// this method previously claimed to do this in its own doc comment without actually doing it,
    /// caught live reviewing corpus-report output (2026-09-08) rather than by any test, since none of
    /// today's patterns happen to require an exact space at a position real text puts an NBSP. Public
    /// (not private) so a caller comparing/grouping raw corpus text - e.g. the corpus report tool -
    /// normalizes the exact same way this classifier does, rather than treating two texts as distinct
    /// inputs that <see cref="Classify"/> itself would treat identically.</summary>
    public static string Normalize(string text) => text.Replace('’', '\'').Replace(' ', ' ');

    private static RuleTarget ClassifyTarget(string text)
    {
        var markupMatch = MarkupKeywordPhrase().Match(text);
        if (markupMatch.Success)
            return new KeywordRuleTarget(markupMatch.Groups[1].Value.ToUpperInvariant());

        var allCapsMatch = AllCapsKeywordPhrase().Match(text);
        if (allCapsMatch.Success)
            return new KeywordRuleTarget(allCapsMatch.Groups[1].Value);

        if (AttachedUnitPhrase().IsMatch(text))
            return new AttachedUnitRuleTarget();

        return new SelfRuleTarget();
    }

    private static List<CharacteristicEffect> ClassifyEffects(string text)
    {
        var effects = new List<CharacteristicEffect>();

        var invulnerableSaveMatch = InvulnerableSaveGrant().Match(text);
        if (invulnerableSaveMatch.Success)
            effects.Add(new CharacteristicEffect("InSv", EffectVerb.Set,
                int.Parse(invulnerableSaveMatch.Groups[1].Value)));

        foreach (Match match in AddCharacteristic().Matches(text))
        {
            if (CharacteristicNames.TryGetValue(match.Groups[2].Value.Trim(), out var characteristic))
                effects.Add(new CharacteristicEffect(characteristic, EffectVerb.Improve,
                    int.Parse(match.Groups[1].Value)));
        }

        return effects;
    }
}