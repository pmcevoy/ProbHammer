using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone - mitigates
/// design.md's Risk: "the '\"Detachment\"' selection-group name match is unverified across the full
/// corpus. Confirmed real in Space Marines... but not exhaustively checked against every faction
/// file in the live clone." Same pattern as CharacteristicResolutionScanTests/WeaponKeywordScanTests:
/// every real faction file (excluding "Library" files - shared cross-sub-faction content, never a
/// playable faction identity itself, matching BsdataFactionResolver's own exclusion) gets a turn as
/// its own closure's starting file, and its closure must resolve at least one Detachment entry via
/// BsdataNameResolver.ResolveDetachmentEntries - a differently-named outlier is caught here rather
/// than failing silently (a resolution producing zero Detachments) in production.
/// </summary>
public class DetachmentGroupNameScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_detachment_group_name_scan()
    {
        var source = LiveClone.RequireSource();

        var failures = new List<string>();

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            if (fileName.Contains("Library", StringComparison.OrdinalIgnoreCase))
                continue;

            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var detachmentEntries = BsdataNameResolver.ResolveDetachmentEntries(closure);

            if (detachmentEntries.Count == 0)
                failures.Add(fileName);
        }

        AllowlistCheck.AssertClean(failures, DetachmentGroupNameAllowlist.Entries, fileName => fileName);
    }
}
