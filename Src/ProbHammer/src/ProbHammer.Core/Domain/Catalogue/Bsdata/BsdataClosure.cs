using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// The full transitive set of catalogue files reachable from one starting file, in resolution
/// order: the starting file first, then every imported file, nearer files before farther ones
/// (see design.md's "Local-file entries always win" decision). Name/id resolution always walks
/// <see cref="Files"/> in this order and stops at the first match.
/// </summary>
public sealed class BsdataClosure(IReadOnlyList<(string FileName, BsCatalogue Catalogue)> files)
{
    public IReadOnlyList<(string FileName, BsCatalogue Catalogue)> Files { get; } = files;
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
        }

        return new BsdataClosure(order);
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
