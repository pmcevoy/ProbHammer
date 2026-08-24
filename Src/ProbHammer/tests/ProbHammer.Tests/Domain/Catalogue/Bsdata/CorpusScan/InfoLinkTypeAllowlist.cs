namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Allowlist for <see cref="InfoLinkTypeScanTests"/> - the `infoLink` `type` values
/// `BsdataDatasheetMapper.WalkInfoLink` currently recognizes and handles, plus one confirmed,
/// deliberately-not-yet-fixed gap. Any other value showing up in the corpus is a new, unhandled
/// shape (the same class of gap `"rule"` was before resolve-core-rule-abilities) and must be
/// triaged deliberately, not silently dropped.
/// </summary>
public static class InfoLinkTypeAllowlist
{
    public static IReadOnlyList<AllowlistEntry<string>> Entries { get; } =
    [
        new("'profile' - resolved into a shared statline/weapon/ability profile via ProfileIdIndex.", t => t == "profile"),
        new("'rule' - resolved into a Core/faction rule reference Ability via RuleGlossary (resolve-core-rule-abilities).", t => t == "rule"),

        // Confirmed real gap, found by this same scan on resolve-core-rule-abilities' first run
        // against the live clone (129 occurrences) - deliberately NOT fixed by this change (see
        // that change's design.md/PROGRESS.md). Targets a `sharedInfoGroups`/`infoGroups`
        // container holding its own nested `profiles` array of real Abilities-typeName content
        // (confirmed real example: Adeptus Custodes' "Talons" info group holds two genuine, real-
        // text, mutually-exclusive-by-modifier auras - "Null Aegis (Aura)" and "Deadly Unity
        // (Aura)" - currently invisible on /LivePlay, the same class of bug resolve-core-rule-
        // abilities fixed for "rule"). Not folded into that change because verifying it needs a
        // real Custodes (or other infoGroup-carrying faction) export to check against, which
        // wasn't available at the time - tracked as a dedicated follow-up instead of fixed blind.
        new(
            "'infoGroup' - confirmed real ability-granting gap (Adeptus Custodes 'Talons' and " +
            "similar), tracked as a dedicated follow-up change once a real export to verify " +
            "against is available - not fixed by resolve-core-rule-abilities.",
            t => t == "infoGroup"),
    ];
}
