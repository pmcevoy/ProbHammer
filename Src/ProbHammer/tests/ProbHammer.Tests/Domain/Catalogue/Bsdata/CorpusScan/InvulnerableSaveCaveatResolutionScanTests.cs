using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan (resolve-known-ability-effects) confirming
/// <see cref="InvulnerableSaveCaveatClassifier"/>'s known-template set still covers every real
/// footnoted InSv reachable across the live BSData clone - every real still-caveated result after
/// classification must be a known, allowlisted anomaly (e.g. Orks' Makari, whose linked ability
/// restricts re-rolls rather than naming an attack-type split), not a missed template. Mirrors
/// <see cref="CharacteristicResolutionScanTests"/>'s own walk exactly, but inspects the resulting
/// Datasheet's own Statlines rather than the exceptions BuildDatasheet throws - a resolution failure
/// unrelated to InSv is that other scan's concern, so this one only ever looks at entries that built
/// successfully.
/// </summary>
public class InvulnerableSaveCaveatResolutionScanTests
{
    public sealed record StillCaveatedResult(string FileName, string EntryName, string StatlineName, string AbilityText);

    [Fact(Explicit = true)]
    public void Full_corpus_invulnerable_save_caveat_resolution_scan()
    {
        var source = LiveClone.RequireSource();

        var results = new List<StillCaveatedResult>();

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);

            var startingCatalogue = closure.Files[0].Catalogue;
            foreach (var entry in startingCatalogue.SharedSelectionEntries)
            {
                Datasheet datasheet;
                try
                {
                    datasheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
                }
                catch (Exception)
                {
                    continue; // a resolution failure here is CharacteristicResolutionScanTests' own concern.
                }

                foreach (var (statlineName, statline) in datasheet.Statlines)
                {
                    if (statline.InSv.Caveated)
                        results.Add(new StillCaveatedResult(
                            fileName, entry.Name, statlineName, statline.InSv.CaveatAbility!.Text));
                }
            }
        }

        AllowlistCheck.AssertClean(results, InvulnerableSaveCaveatResolutionAllowlist.Entries,
            r => $"{r.FileName} :: '{r.EntryName}' / '{r.StatlineName}' -> \"{r.AbilityText}\"");
    }
}
