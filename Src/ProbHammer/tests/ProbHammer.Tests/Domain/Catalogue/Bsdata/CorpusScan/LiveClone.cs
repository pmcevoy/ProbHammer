using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Shared access point for both full-corpus scan tests to the local BSData clone
/// (`C:\Users\Pete\wh40k-11e`, this machine only, not part of the repo - the same literal path
/// already documented in CLAUDE.md, .claude/bsdata-json-schema.md, and
/// .claude/domain-model-11e.md, so hardcoding it here introduces no new leak; see design.md's
/// "Live-clone detection" decision). <see cref="RequireSource"/> is the only way either scan
/// touches <see cref="LocalDiskBsdataCatalogueSource"/>, so the clone-absent case is handled in
/// exactly one place.
/// </summary>
public static class LiveClone
{
    public const string ClonePath = @"C:\Users\Pete\wh40k-11e";

    /// <summary>BSData's own catalogue for the game itself, not a faction/sub-faction file -
    /// fails to deserialize as a <c>catalogueSchema</c> document and is unrelated to what either
    /// scan checks (see PROGRESS.md's Known Issues).</summary>
    private const string ExcludedFileName = "Warhammer 40,000.json";

    /// <summary>
    /// Returns a source over the local clone, or calls <see cref="Assert.Skip"/> (xUnit v3's
    /// dynamic skip - reports the test as skipped, not passed, per this capability's "No-Op
    /// Without Local Clone" requirement) when the clone isn't present on this machine.
    /// </summary>
    public static LocalDiskBsdataCatalogueSource RequireSource()
    {
        if (!Directory.Exists(ClonePath))
            Assert.Skip($"Local BSData clone not found at '{ClonePath}' - this scan only runs on a machine with the clone present.");

        return new LocalDiskBsdataCatalogueSource(ClonePath);
    }

    /// <summary>Every real catalogue file name in the clone, excluding <see cref="ExcludedFileName"/>.</summary>
    public static IReadOnlyList<string> CatalogueFileNames(LocalDiskBsdataCatalogueSource source) =>
        source.ListFileNames()
            .Where(name => !string.Equals(name, ExcludedFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
