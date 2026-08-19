namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Local-disk implementation of <see cref="IBsdataCatalogueSource"/>, reading files directly from
/// a root directory supplied by the caller (e.g. sourced from appsettings.json or an environment
/// variable at the caller's composition root) - never a hardcoded path, so no specific developer's
/// machine leaks into checked-in code. This change reads a local BSData clone during development
/// to avoid GitHub API rate limits (see design.md); an HTTP-fetching source can implement the same
/// interface later without changing anything downstream of it.
/// </summary>
public sealed class LocalDiskBsdataCatalogueSource(string rootDirectory) : IBsdataCatalogueSource
{
    public string GetJson(string fileName) =>
        File.ReadAllText(Path.Combine(rootDirectory, fileName));

    public IReadOnlyList<string> ListFileNames() =>
        Directory.GetFiles(rootDirectory, "*.json")
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();
}
