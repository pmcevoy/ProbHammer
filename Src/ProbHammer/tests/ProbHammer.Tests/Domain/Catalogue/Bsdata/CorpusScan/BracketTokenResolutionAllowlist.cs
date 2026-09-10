namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// "Known limitation" allowlist for <see cref="BracketTokenResolutionScanTests"/>. Two genuine
/// one-off BSData authoring anomalies remain unresolved by `RuleGlossary`'s normalized-key
/// resolution, neither fixed here.
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
        // English prose, not a cross-reference at all. A known false-positive-extraction risk,
        // degrading exactly as designed: this token simply renders as inert, non-interactive text.
        new(
            "'whichever applies' (Blood Angels' 'Visions of Heresy') - ordinary bracketed prose, " +
            "not a cross-reference; the confirmed real instance of design.md's stray-bracket risk.",
            t => t.Token == "whichever applies"),
    ];
}
