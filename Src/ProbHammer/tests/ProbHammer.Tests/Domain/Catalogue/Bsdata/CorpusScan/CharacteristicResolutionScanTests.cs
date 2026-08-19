using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone - see
/// design.md's "Corpus walk" decision. Every catalogue file gets a turn as the starting file for
/// its own closure resolution, and every one of that file's own top-level
/// SharedSelectionEntries is attempted through BsdataDatasheetMapper.BuildDatasheet, which
/// recursively walks that entry's whole subtree (nested entries, groups, entryLinks, infoLinks),
/// so this covers every characteristic reachable from any locally-defined entry across the whole
/// corpus, not just names a captured export happens to reference.
/// </summary>
public class CharacteristicResolutionScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_characteristic_resolution_scan()
    {
        var source = LiveClone.RequireSource();

        var failures = new List<Exception>();
        var descriptions = new Dictionary<Exception, string>();

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);

            var startingCatalogue = closure.Files[0].Catalogue;
            foreach (var entry in startingCatalogue.SharedSelectionEntries)
            {
                try
                {
                    BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                    descriptions[ex] = $"{fileName} :: '{entry.Name}' -> {ex.GetType().Name}: {ex.Message}";
                }
            }
        }

        AllowlistCheck.AssertClean(failures, CharacteristicResolutionAllowlist.Entries, ex => descriptions[ex]);
    }
}
