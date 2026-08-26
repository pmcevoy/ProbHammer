namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// "Known limitation" allowlist for <see cref="ArmyRuleNameResolutionScanTests"/> - every entry in
/// <c>ArmyRuleNameLookup</c>'s inclusion table was verified to resolve against the bundled corpus
/// at the time it was added (see design.md's "Initial table contents"); a future mid-edition
/// rename that breaks a table entry gets a new, explicit entry here (or, more likely, a fix to the
/// table itself) rather than the scan being silently ignored. The entries below are NOT table
/// content bugs - every name is confirmed correct (verified directly against the raw BSData JSON
/// and, for Power from Pain, against DetachmentGroupNameAllowlist's own pre-existing finding for
/// the identical root cause) - they document a pre-existing, already out-of-scope
/// BsdataClosureResolver limitation: the owning faction's own closure genuinely cannot reach the
/// shared library file the rule is declared in, because that catalogueLink's own
/// importRootEntries is false in the source data (the exact same gap DetachmentGroupNameAllowlist
/// already documents for Aeldari - Drukhari.json/Tyranids.json's own Detachment-group resolution -
/// this scan just surfaces the identical root cause from a different angle, and, for Astra
/// Militarum below, from yet another angle DetachmentGroupNameAllowlist never had reason to touch,
/// since that faction's own Detachment group resolves fine locally). Fixing which links
/// BsdataClosureResolver follows is a deep, high-blast-radius change unrelated to this proposal's
/// own scope, same reasoning as that existing allowlist. In production this means these rosters'
/// Core Rule Ability Extraction silently produces no Ability for these specific rules via
/// the BSData text pipeline today (mirrors ProcessRuleInfoLink's own "unresolvable rule name...
/// silently produce no Ability" convention) - the BattleScribe/JSON pipeline is unaffected, since
/// it reads a roster's own already-resolved rule text directly with no BSData closure involved at
/// all (confirmed: data/gw-android-export-deathguard.json's own "Nurgle's Gift (Aura)" entry
/// resolves fine there). See PROGRESS.md's Known Issues.
/// </summary>
public static class ArmyRuleNameResolutionAllowlist
{
    public static IReadOnlyList<AllowlistEntry<(string Faction, string Name)>> Entries { get; } =
    [
        new(
            "Drukhari :: 'Power from Pain' - declared in 'Aeldari - Aeldari Library.json', which " +
            "Drukhari's own catalogueLink does not import (importRootEntries: false) - see " +
            "DetachmentGroupNameAllowlist's identical 'Aeldari - Drukhari.json' entry.",
            f => f.Faction == "Drukhari" && f.Name == "Power from Pain"),
        new(
            "Tyranids :: 'Synapse' - declared in 'Library - Tyranids.json', which Tyranids.json's " +
            "own catalogueLink does not import (importRootEntries: false) - see " +
            "DetachmentGroupNameAllowlist's identical 'Tyranids.json' entry.",
            f => f.Faction == "Tyranids" && f.Name == "Synapse"),
        new(
            "Tyranids :: 'Shadow in the Warp' - same cause as 'Synapse' above.",
            f => f.Faction == "Tyranids" && f.Name == "Shadow in the Warp"),
        new(
            "Astra Militarum :: 'Voice Of Command' - declared in 'Imperium - Astra Militarum - " +
            "Library.json', which Astra Militarum's own catalogueLink does not import " +
            "(importRootEntries: false) - same root cause as the two entries above, found by " +
            "classify-known-army-rules' own Full-Faction Coverage validation rather than by " +
            "DetachmentGroupNameScanTests (Astra Militarum's own Detachment group resolves fine " +
            "locally, so that scan had no reason to surface this file at all).",
            f => f.Faction == "Astra Militarum" && f.Name == "Voice Of Command")
    ];
}