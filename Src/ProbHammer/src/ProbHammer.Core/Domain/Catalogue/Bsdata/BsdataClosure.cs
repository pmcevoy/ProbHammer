using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// The full transitive set of catalogue files reachable from one starting file, in resolution
/// order: the starting file first, then every imported file, nearer files before farther ones
/// (see design.md's "Local-file entries always win" decision). Name/id resolution always walks
/// <see cref="Files"/> in this order and stops at the first match.
/// </summary>
public sealed class BsdataClosure(IReadOnlyList<(string FileName, BsCatalogue Catalogue)> files, BsCatalogue? gameSystem = null)
{
    public IReadOnlyList<(string FileName, BsCatalogue Catalogue)> Files { get; } = files;

    /// <summary>The single game-system file every catalogue in <see cref="Files"/> implicitly
    /// depends on (referenced by <see cref="BsCatalogue.GameSystemId"/>, e.g.
    /// "Warhammer 40,000.json") - deliberately kept separate from <see cref="Files"/> rather than
    /// folded into it. Its own SharedSelectionEntries/SharedSelectionEntryGroups are generic,
    /// cross-faction template content (Warlord traits, Enhancements, Crusade rules) this loader
    /// was never designed to walk as if they were real datasheet weapons/statlines - confirmed by
    /// a real corpus-scan failure when this was tried (a T'au Empire entry's dangling "Warlord"/
    /// "Enhancements"/"Crusade" entryLinks, previously silently unresolved, newly resolved into
    /// generic wargear with a literal "*" placeholder value this loader's weapon parsing can't
    /// handle). Only <see cref="BsCatalogue.SharedProfiles"/> is actually needed from here - the
    /// base "Invulnerable Save (X+*)" abilities a footnoted invulnerable save links to - so only
    /// that is exposed, via <see cref="BsdataNameResolver.BuildProfileIdIndex"/>; ordinary
    /// entryLink/link resolution (BuildIdIndex/BuildGroupIdIndex/Resolve) never sees this
    /// catalogue's own entries/groups, preserving the pre-existing "unresolvable target is
    /// silently skipped" behavior for links that were always meant to dangle.</summary>
    public BsCatalogue? GameSystem { get; } = gameSystem;
}

/// <summary>
/// Resolves a faction's full <c>catalogueLinks</c> closure: starting from one file, follows every
/// <c>importRootEntries: true</c> link outward, transitively. Per-faction, not corpus-wide (see
/// design.md's "Resolve per-faction closures" decision) - two unrelated factions' same-named
/// entries are never both in scope for one resolution.
/// </summary>
public static class BsdataClosureResolver
{
    public static BsdataClosure Resolve(IBsdataCatalogueSource source, string startingFileName)
    {
        var availableFiles = new HashSet<string>(source.ListFileNames(), StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<(string FileName, BsCatalogue Catalogue)>();
        var queue = new Queue<string>();

        queue.Enqueue(startingFileName);
        visited.Add(startingFileName);

        BsCatalogue? gameSystem = null;

        while (queue.Count > 0)
        {
            var fileName = queue.Dequeue();
            var catalogue = BsdataCatalogueReader.Read(source.GetJson(fileName));
            order.Add((fileName, catalogue));

            foreach (var link in catalogue.CatalogueLinks.Where(l => l.ImportRootEntries))
            {
                var importedFileName = ResolveImportFileName(source, availableFiles, link);
                if (importedFileName != null && visited.Add(importedFileName))
                    queue.Enqueue(importedFileName);
            }

            // Every catalogue implicitly depends on its one game-system file too, referenced by
            // GameSystemId rather than a CatalogueLinks entry - resolved once per closure (every
            // file in a real closure shares the same game system, so only the first file carrying
            // a non-empty GameSystemId needs to trigger this). Deliberately not enqueued into the
            // same BFS/Files list - see BsdataClosure.GameSystem's own doc comment for why.
            if (gameSystem is null && !string.IsNullOrEmpty(catalogue.GameSystemId))
                gameSystem = ResolveGameSystem(source, availableFiles, catalogue.GameSystemId);
        }

        return new BsdataClosure(order, gameSystem);
    }

    /// <summary>Finds and reads the one file whose own <c>id</c> matches a catalogue's
    /// <c>gameSystemId</c> (e.g. "Warhammer 40,000.json", the base ruleset every real 40k
    /// catalogue references). Unlike a catalogueLink, a gameSystemId reference carries no cached
    /// name to guess from, so this always scans every available file's own id - a
    /// one-time-per-closure cost, matching <see cref="ResolveImportFileName"/>'s existing
    /// fallback for a drifted catalogueLink name.</summary>
    private static BsCatalogue? ResolveGameSystem(
        IBsdataCatalogueSource source, HashSet<string> availableFiles, string gameSystemId)
    {
        foreach (var fileName in availableFiles)
        {
            var catalogue = BsdataCatalogueReader.Read(source.GetJson(fileName));
            if (string.Equals(catalogue.Id, gameSystemId, StringComparison.OrdinalIgnoreCase))
                return catalogue;
        }

        return null;
    }

    /// <summary>A catalogueLink's <c>name</c> usually matches its target file's name directly
    /// (e.g. "Imperium - Space Marines" -> "Imperium - Space Marines.json"), but that cached name
    /// can drift from the target's actual current file name (observed in real data: a link named
    /// "Chaos - Daemons Library" whose target file is actually
    /// "Chaos - Chaos Daemons Library.json") - the link's <c>targetId</c> is the only value
    /// guaranteed to still identify the right file. Try the fast name-based guess first; fall back
    /// to scanning every available file's own top-level catalogue id for a match only when the
    /// guess isn't present.</summary>
    private static string? ResolveImportFileName(
        IBsdataCatalogueSource source, HashSet<string> availableFiles, BsCatalogueLink link)
    {
        var guess = link.Name + ".json";
        if (availableFiles.Contains(guess))
            return guess;

        foreach (var fileName in availableFiles)
        {
            var catalogue = BsdataCatalogueReader.Read(source.GetJson(fileName));
            if (string.Equals(catalogue.Id, link.TargetId, StringComparison.OrdinalIgnoreCase))
                return fileName;
        }

        return null;
    }
}
