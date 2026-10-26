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
        // Iron Hands' "Cold and Calculating" reads "[Sustained Hits 1: non-[Monster/Vehicle]": a stray
        // opening bracket in the source data makes "Monster/Vehicle" its own token.
        new(
            "'Monster/Vehicle' (Iron Hands' 'Cold and Calculating') - a stray '[' in the source " +
            "text splits it out of '[Sustained Hits 1: non-Monster/Vehicle]'.",
            t => t.Token == "Monster/Vehicle"),

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
