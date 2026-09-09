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
            // "Move" is the abbreviated form real corpus text sometimes uses in place of the full
            // "Movement" name - a second key for the same value, not a Normalize()-level rewrite,
            // since this is a characteristic-name synonym, not authoring-variance cleanup.
            ["Move"] = "M",
            ["Toughness"] = "T",
            ["Save"] = "Sv",
            ["Wounds"] = "W",
            ["Leadership"] = "Ld",
            ["Objective Control"] = "Oc"
        };

    /// <summary>The six Statline scalar codes as they appear written bare (not spelled out) in a
    /// shorthand "+N Code" grant, e.g. Marshal's Household's "+1 OC" - keyed case-insensitively since
    /// real corpus text capitalizes these inconsistently, same as every other word-literal pattern in
    /// this classifier (<see cref="AllCapsKeywordPhrase"/> excepted). Values are the canonical,
    /// correctly-cased codes <see cref="ScalarCharacteristicEffect.Characteristic"/> expects.</summary>
    private static readonly Dictionary<string, string> CharacteristicCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["M"] = "M",
            ["T"] = "T",
            ["Sv"] = "Sv",
            ["W"] = "W",
            ["Ld"] = "Ld",
            ["Oc"] = "Oc"
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
    /// character, never a subordinate clause. Still deliberately excludes a match immediately followed
    /// by "against" (e.g. "...invulnerable save against ranged attacks") via negative lookahead - not
    /// because a ranged/melee-restricted grant is unrecognized any more
    /// (resolve-invulnerable-save-effects added <see cref="InvulnerableSaveRangedRestricted"/>/
    /// <see cref="InvulnerableSaveMeleeRestricted"/>, tried first in <see cref="ClassifyEffects"/> and
    /// short-circuiting this pattern whenever either matches - see that change's design.md D4), but
    /// because a restriction on any OTHER axis (e.g. "...invulnerable save against Psychic Attacks")
    /// must still extract nothing at all, per that requirement's own "scoped to the melee/ranged
    /// attack-type axis only" text - this lookahead is what keeps this pattern from wrongly treating
    /// that as an unconditional flat grant once it reaches this fallback. Confirmed real and previously
    /// a false positive before this exclusion existed: "This model has a 4+ invulnerable save against
    /// ranged attacks..." (Ensorcelled Shield) and "The bearer has a 4+ invulnerable save against
    /// ranged attacks, and a 5+ invulnerable save against melee attacks." (Veil of Medrengard) both
    /// wrongly extracted a flat Set InSv - caught live reviewing corpus-report output (2026-09-08), not
    /// by any test; both now correctly extract via the two restricted patterns instead of via this
    /// one.</summary>
    [GeneratedRegex(SentenceStart + @"[A-Za-z][A-Za-z''\- ]{0,60} has an? (\d+)\+ invulnerable save(?!\s+against\b)",
        RegexOptions.IgnoreCase)]
    private static partial Regex InvulnerableSaveGrant();

    /// <summary>Either a true sentence start with a subject-shaped prefix and "has"/"have" (the
    /// one-sided grant shape, e.g. "This model has a 4+ invulnerable save against ranged attacks." or
    /// the plural "Models in this unit have a 4+ invulnerable save against ranged attacks." -
    /// Howling Banshees, confirmed real via the live BSData clone; the same claim stated from a whole
    /// unit's own perspective rather than a single bearer's, mirroring
    /// <see cref="InvulnerableSaveCaveatClassifier"/>'s own bare/unit template pairing so retiring that
    /// mapper-time classifier in favor of this one doesn't regress this real corpus shape - see
    /// widen-baseline-generation-coverage/design.md), or a ", and" continuation from an earlier clause
    /// in the same sentence with an elided verb (the two-sided grant shape - confirmed real via the
    /// live BSData clone, Veil of Medrengard's own melee clause below: "...against ranged attacks, and
    /// a 5+ invulnerable save against melee attacks." is grammatically "...and [has] a 5+..."). See
    /// resolve-invulnerable-save-effects/design.md's D4.</summary>
    [GeneratedRegex(
        "(?:" + SentenceStart +
        @"[A-Za-z][A-Za-z''\- ]{0,60} (?:has|have)|, and) an? (\d+)\+ invulnerable save against ranged attacks",
        RegexOptions.IgnoreCase)]
    private static partial Regex InvulnerableSaveRangedRestricted();

    /// <summary>The melee-axis counterpart to <see cref="InvulnerableSaveRangedRestricted"/> - same
    /// three-alternative lead-in (singular "has", plural "have", or the elided-verb continuation), same
    /// rationale. Confirmed real via both the one-sided shape (Ensorcelled Shield: "This model has a
    /// 4+ invulnerable save against ranged attacks..." has no melee clause at all, so this pattern
    /// correctly finds no match there) and the two-sided shape (Veil of Medrengard's own melee clause,
    /// matched via the ", and" alternative); the plural form mirrors
    /// <see cref="InvulnerableSaveRangedRestricted"/>'s own Howling Banshees rationale.</summary>
    [GeneratedRegex(
        "(?:" + SentenceStart +
        @"[A-Za-z][A-Za-z''\- ]{0,60} (?:has|have)|, and) an? (\d+)\+ invulnerable save against melee attacks",
        RegexOptions.IgnoreCase)]
    private static partial Regex InvulnerableSaveMeleeRestricted();

    /// <summary>The optional non-capturing <c>(?:[A-Za-z]+'s\s+)?</c> group between "the" and the
    /// characteristic name recognizes a possessive noun ("the bearer's Wounds characteristic"), not
    /// just the plain phrasing ("the Wounds characteristic") the original pattern required - a
    /// generic structural widening (any possessive noun), not a "bearer's"-specific denylist entry,
    /// matching this classifier's established preference for structural anchors over phrase lists
    /// (<see cref="SentenceStart"/>, <see cref="AttachedUnitPhrase"/>'s own widening). Confirmed real,
    /// previously-dropped examples: Blasphemous Engine and Da Krushin' Armour ("Add N to the bearer's
    /// Wounds characteristic.") and the first clause of Master Artisan. Group numbering is unaffected
    /// - the possessive group is non-capturing, so group 1 (amount) and group 2 (characteristic name)
    /// are the same as before this widening.</summary>
    [GeneratedRegex(SentenceStart + @"Add (\d+) to the(?:\s+[A-Za-z]+'s)? ([A-Za-z ]+?) characteristic",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddCharacteristic();

    /// <summary>A shorthand "+N Code" grant (e.g. Marshal's Household's "Friendly SWORD BRETHREN SQUAD
    /// units have +1 OC."), recognized alongside the spelled-out "Add N to the X characteristic"
    /// phrasing <see cref="AddCharacteristic"/> already covers. Requires a subject-shaped run of text
    /// (mirroring <see cref="InvulnerableSaveGrant"/>'s own subject requirement) followed by "have"/
    /// "has", then the shorthand grant itself, one of the six Statline scalar codes as a whole word
    /// (see <see cref="CharacteristicCodes"/>) with an optional intervening "to" (covers both "+1 OC"
    /// and a possible "+1 to OC" variant). Anchored by <see cref="SentenceStart"/> like every other
    /// Effect pattern, for the same reason: an unanchored version could match a shorthand increment
    /// embedded in a still-conditional clause. The subject-run character class deliberately excludes
    /// a comma (matching <see cref="InvulnerableSaveGrant"/>'s own class exactly) - an early draft
    /// allowed one and, caught immediately by its own negative test, let the subject run span an
    /// entire comma-joined conditional preamble ("If it does, until the end of the phase, this unit
    /// has +1 OC."), defeating the SentenceStart anchor the same way an unbounded subject class
    /// always would.</summary>
    [GeneratedRegex(
        SentenceStart + @"[A-Za-z][A-Za-z''\- ]{0,80} (?:have|has) \+(\d+)(?:\s+to)?\s+(M|T|Sv|W|Ld|Oc)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ShorthandCharacteristicPlus();

    public static RuleClassification Classify(string name, string text)
    {
        var normalized = Normalize(text);
        var (target, targetMatch) = ClassifyTarget(normalized);
        var (effects, effectMatches) = ClassifyEffects(normalized);
        var isCaveated = effects.Count > 0 && IsCaveated(normalized, targetMatch, effectMatches);

        return new RuleClassification(target, effects, isCaveated);
    }

    /// <summary>The validated structural caveat signal (see widen-rule-effect-classification-coverage
    /// design.md's "Caveat signal computed from raw match end positions" decision): the end position
    /// of the LAST regex match that contributed to either the classified Target or an extracted
    /// Effect, with whatever text remains after it (trimmed of whitespace, then a single trailing
    /// period) checked for emptiness. A non-empty remainder means the text states real content beyond
    /// what Target/Effects captured - this method never attempts to classify what that content is,
    /// only that it exists. Only called when at least one Effect was extracted - see
    /// <see cref="Classify"/> - since the hand-validated 5-caveated/15-clean split this mirrors was
    /// only checked against results with 1+ Effects.</summary>
    private static bool IsCaveated(string text, Match? targetMatch, IReadOnlyList<Match> effectMatches)
    {
        var lastEnd = effectMatches.Select(m => m.Index + m.Length).DefaultIfEmpty(0).Max();
        if (targetMatch is { } match)
            lastEnd = Math.Max(lastEnd, match.Index + match.Length);

        var remainder = text[lastEnd..].Trim();
        if (remainder.EndsWith('.'))
            remainder = remainder[..^1].TrimEnd();

        return remainder.Length > 0;
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

    /// <summary>Returns the classified Target alongside the specific <see cref="Match"/> that produced
    /// it (null for the <see cref="SelfRuleTarget"/> fallback, which has no contributing match) - the
    /// Match's own end position feeds <see cref="IsCaveated"/>.</summary>
    private static (RuleTarget Target, Match? Match) ClassifyTarget(string text)
    {
        var markupMatch = MarkupKeywordPhrase().Match(text);
        if (markupMatch.Success)
            return (new KeywordRuleTarget(markupMatch.Groups[1].Value.ToUpperInvariant()), markupMatch);

        var allCapsMatch = AllCapsKeywordPhrase().Match(text);
        if (allCapsMatch.Success)
            return (new KeywordRuleTarget(allCapsMatch.Groups[1].Value), allCapsMatch);

        var attachedUnitMatch = AttachedUnitPhrase().Match(text);
        if (attachedUnitMatch.Success)
            return (new AttachedUnitRuleTarget(), attachedUnitMatch);

        return (new SelfRuleTarget(), null);
    }

    private const string InvulnerableSaveTrigger = "invulnerable save";

    /// <summary>D5's cheap, provably-safe pre-filter gate ahead of the whole invulnerable-save
    /// pattern family (<see cref="InvulnerableSaveRangedRestricted"/>/
    /// <see cref="InvulnerableSaveMeleeRestricted"/>/<see cref="InvulnerableSaveGrant"/>): every one
    /// of those patterns already requires the literal substring "invulnerable save" to match at all,
    /// so this is a strict over-approximation - it can never reject a text any InSv pattern would
    /// have accepted, checkable by inspection of the patterns themselves rather than by
    /// corpus-running. See resolve-invulnerable-save-effects/design.md's D5/D8; scoped narrowly to
    /// this family only, not a general "every family gets a gate" redesign. Takes already-
    /// <see cref="Normalize"/>d text, matching this class's other private helpers - a caller working
    /// from raw text should call <see cref="MayStateInvulnerableSave"/> instead.</summary>
    private static bool ContainsInvulnerableSaveTrigger(string normalizedText) =>
        normalizedText.Contains(InvulnerableSaveTrigger, StringComparison.OrdinalIgnoreCase);

    /// <summary>Public, raw-text-accepting wrapper around the same D5 gate <see cref="ClassifyEffects"/>
    /// uses internally - lets a caller working from raw corpus text (e.g. the corpus report tool, task
    /// 5.1) distinguish a text that could never plausibly state an invulnerable-save effect from one
    /// that could but matched no recognized pattern, without duplicating the gate's own
    /// substring/normalization logic.</summary>
    public static bool MayStateInvulnerableSave(string text) => ContainsInvulnerableSaveTrigger(Normalize(text));

    /// <summary>Returns the extracted Effects alongside the specific <see cref="Match"/> that produced
    /// each one, in the same order - every contributing Match's own end position feeds
    /// <see cref="IsCaveated"/>.</summary>
    private static (List<CharacteristicEffect> Effects, List<Match> Matches) ClassifyEffects(string text)
    {
        var effects = new List<CharacteristicEffect>();
        var matches = new List<Match>();

        // The two restricted patterns are tried first; the uniform InvulnerableSaveGrant pattern
        // (which excludes any "against"-followed match, including a restriction on some OTHER axis
        // like Psychic Attacks - see that pattern's own doc comment) is only attempted as a fallback
        // when neither restricted pattern matched, avoiding double-extraction of the same clause -
        // see resolve-invulnerable-save-effects/design.md's D4.
        if (ContainsInvulnerableSaveTrigger(text))
        {
            var rangedMatch = InvulnerableSaveRangedRestricted().Match(text);
            var meleeMatch = InvulnerableSaveMeleeRestricted().Match(text);

            if (rangedMatch.Success || meleeMatch.Success)
            {
                var ranged = rangedMatch.Success ? int.Parse(rangedMatch.Groups[1].Value) : 0;
                var melee = meleeMatch.Success ? int.Parse(meleeMatch.Groups[1].Value) : 0;
                effects.Add(new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(melee, ranged)));
                if (rangedMatch.Success) matches.Add(rangedMatch);
                if (meleeMatch.Success) matches.Add(meleeMatch);
            }
            else
            {
                var invulnerableSaveMatch = InvulnerableSaveGrant().Match(text);
                if (invulnerableSaveMatch.Success)
                {
                    var value = int.Parse(invulnerableSaveMatch.Groups[1].Value);
                    effects.Add(new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(value, value)));
                    matches.Add(invulnerableSaveMatch);
                }
            }
        }

        foreach (Match match in AddCharacteristic().Matches(text))
        {
            if (!CharacteristicNames.TryGetValue(match.Groups[2].Value.Trim(), out var characteristic))
                continue;

            effects.Add(new ScalarCharacteristicEffect(characteristic, EffectVerb.Improve,
                int.Parse(match.Groups[1].Value)));
            matches.Add(match);
        }

        var shorthandMatch = ShorthandCharacteristicPlus().Match(text);
        if (shorthandMatch.Success &&
            CharacteristicCodes.TryGetValue(shorthandMatch.Groups[2].Value, out var code))
        {
            effects.Add(new ScalarCharacteristicEffect(code, EffectVerb.Improve,
                int.Parse(shorthandMatch.Groups[1].Value)));
            matches.Add(shorthandMatch);
        }

        return (effects, matches);
    }
}