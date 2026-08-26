using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone - see
/// classify-known-army-rules' Full-Faction Coverage Scan decision (design.md). Distinct from
/// <see cref="PrimaryCatalogueGatedRuleTriageScanTests"/>, which only ever discovers a
/// shared-library, gated rule shape (Oath-of-Moment-shaped) - this scan instead enumerates every
/// real playable-faction catalogue file directly and requires <see cref="ArmyRuleNameLookup"/> to
/// carry an explicit entry (a populated name list, or a deliberately empty one for a faction
/// confirmed to have no single unifying army-wide rule) for every one of them, so "no entry"
/// always means "nobody has checked yet" - never "checked and found nothing" - and a brand-new
/// faction added to a future BSData snapshot can't silently sit uncovered the way roughly half the
/// real corpus did when this change's own original 11-faction table shipped.
///
/// Reads <see cref="ArmyRuleNameLookup.Entries"/>, not <see cref="ArmyRuleNameLookup.Resolve"/> -
/// <c>Resolve</c>'s own "absent key or present-but-empty" contract intentionally treats both as an
/// empty result, which is exactly the distinction this scan exists to preserve. No new allowlist/
/// deferral type is introduced - the table's own existing "an empty list is a valid, deliberate
/// entry" contract is the only mechanism.
/// </summary>
public class ArmyRuleNameCoverageScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_faction_coverage_scan()
    {
        var source = LiveClone.RequireSource();

        var knownFactions = ArmyRuleNameLookup.Entries
            .Select(e => e.Faction)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var realFactionFiles = LiveClone.CatalogueFileNames(source)
            .Where(f => !f.Contains("Library", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var missing = realFactionFiles
            .Select(f => (File: f, Faction: DeriveFactionName(f)))
            .Where(x => !knownFactions.Contains(x.Faction))
            .ToList();

        Assert.True(missing.Count == 0,
            "The following real BSData faction catalogue files have no ArmyRuleNameLookup entry " +
            "at all (not even a deliberate empty one) - each needs either a verified inclusion-" +
            "table entry or an explicit empty-list entry once research confirms there's no single " +
            "unifying army-wide rule:\n" +
            string.Join("\n", missing.Select(x => $"  {x.File} -> '{x.Faction}'")));
    }

    /// <summary>Mirrors <c>BsdataFactionResolver.ResolveStartingFileName</c>'s own "most specific
    /// entry" convention in reverse: strips the ".json" extension, then the text after the last
    /// " - " if present, else the whole stem - the same shape every
    /// <see cref="ArmyRuleNameLookup"/> table key already follows.</summary>
    private static string DeriveFactionName(string fileName)
    {
        var stem = fileName[..^".json".Length];
        var separatorIndex = stem.LastIndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex >= 0 ? stem[(separatorIndex + 3)..] : stem;
    }
}
