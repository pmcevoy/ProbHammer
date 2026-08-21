namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// "Known limitation" allowlist for <see cref="BracketTokenResolutionScanTests"/>. The first real
/// run against the live clone (before `RuleGlossary`'s normalized-key resolution existed) found
/// 1,964 unresolved occurrences across 71 distinct tokens - the overwhelming majority a generic
/// mechanic's bare Name/Alias with a value or target category appended (e.g. "SUSTAINED HITS 1",
/// "ANTI-VEHICLE 3+"), plus a smaller group with no `alias` array at all ("Cleave",
/// "Close-quarters" - each self-references itself by Name in its own description text, e.g.
/// Cleave's own text says "**[CLEAVE X]**", the same convention every other generic mechanic
/// uses). `RuleGlossary`'s single normalized-key index (built from both Name and Alias, collapsed
/// through an ordered text-normalization pipeline - see design.md's "Bounded normalization before
/// resolution, not fuzzy matching") now resolves all of that. Re-running the scan afterward
/// confirmed only 2 occurrences remain, both genuine one-off BSData authoring anomalies - neither
/// fixed here.
/// </summary>
public static class BracketTokenResolutionAllowlist
{
    public static IReadOnlyList<AllowlistEntry<(string Token, string Location)>> Entries { get; } =
    [
        // Confirmed real: Adeptus Custodes' "No Foe Shall Stand" ability text reads "...have the
        // [LETHAL HITS] and [IGNORES COVER abilities]." - the closing bracket was placed one word
        // too late in the source data, swallowing "abilities" into the token itself. A genuine
        // single BSData authoring anomaly, not a systematic pattern - "abilities" isn't a
        // recognized value/threshold token, so the normalization pipeline correctly leaves it
        // un-stripped rather than incidentally "fixing" it with a looser rule.
        new(
            "'IGNORES COVER abilities' (Adeptus Custodes' 'No Foe Shall Stand') - the source " +
            "text's closing bracket is misplaced one word late, swallowing 'abilities' into the " +
            "token.",
            t => t.Token == "IGNORES COVER abilities"),

        // Confirmed real: Blood Angels' "Visions of Heresy" ability text ends "...or you can
        // re-roll the Charge roll made for this unit [whichever applies]" - ordinary bracketed
        // English prose, not a cross-reference at all. Exactly the false-positive-extraction risk
        // design.md calls out, degrading exactly as designed: this token simply renders as inert,
        // non-interactive text.
        new(
            "'whichever applies' (Blood Angels' 'Visions of Heresy') - ordinary bracketed prose, " +
            "not a cross-reference; the confirmed real instance of design.md's stray-bracket risk.",
            t => t.Token == "whichever applies"),
    ];
}
