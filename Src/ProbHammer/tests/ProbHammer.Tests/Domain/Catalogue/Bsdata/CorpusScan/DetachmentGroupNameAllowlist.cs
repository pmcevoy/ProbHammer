using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>Allowlist for <see cref="DetachmentGroupNameScanTests"/> - confirmed real corpus gaps
/// found by the scan's first real run against the live clone. Started 15 failures against the
/// original single-shape ("a top-level selectionEntryGroup literally named 'Detachment'")
/// assumption design.md flagged as unverified; <see cref="BsdataNameResolver.ResolveDetachmentEntries"/>
/// was generalized to a second, equally-common real shape (a wrapper entry of the same name holding
/// one nested group), which fixed 11 of the 15. These 4 remain unresolvable with the two shapes that
/// method now recognizes, all for the SAME underlying reason: the real Detachment choices live
/// inside a "Library" catalogue file this specific faction's own closure never actually reaches,
/// because the catalogueLink to that Library has `importRootEntries` false in the source data
/// (confirmed by direct inspection - see each entry's own note) - a closure-resolution gap, not a
/// Detachment-group-shape gap, and out of scope to fix here (widening which links
/// BsdataClosureResolver follows is a deep, high-blast-radius change unrelated to this proposal's
/// own scope). Mirrors this project's existing "infoGroup" precedent
/// (InfoLinkTypeAllowlist.cs) - a confirmed, real, not-yet-fixed gap, deliberately allowlisted
/// rather than guessed around, tracked as a dedicated follow-up.</summary>
public static class DetachmentGroupNameAllowlist
{
    public static IReadOnlyList<AllowlistEntry<string>> Entries { get; } =
    [
        new(
            "Aeldari - Drukhari.json - its own catalogueLink to 'Aeldari - Aeldari Library' (which " +
            "holds the real 'Detachments' group, confirmed real - includes 'Warhost'/'Armoured " +
            "Warhost') has no importRootEntries:true, unlike Craftworlds' identical-target link, " +
            "which does and resolves fine.",
            f => f == "Aeldari - Drukhari.json"),
        new(
            "Tyranids.json - its own top-level 'Detachment' wrapper entry's sole nested group has " +
            "no name and no entries of its own, only an entryLink into 'Library - Tyranids' - that " +
            "catalogueLink also has no importRootEntries:true, so the target file (which does hold " +
            "the real Detachment choices) is never part of this closure.",
            f => f == "Tyranids.json"),
        new(
            "Genestealer Cults.json - the identical shape/cause as Tyranids.json, its own wrapper " +
            "entry's nested group reaching an unimported Library file via entryLink.",
            f => f == "Genestealer Cults.json"),
        new(
            "Unaligned Forces.json - carries no Detachment content of its own under either shape, " +
            "and none of the factions that import it (e.g. Tyranids) can reach the real choices " +
            "through it either.",
            f => f == "Unaligned Forces.json"),
    ];
}