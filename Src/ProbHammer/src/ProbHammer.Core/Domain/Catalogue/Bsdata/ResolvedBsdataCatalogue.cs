using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// A resolved closure bundled with the id/group/profile indices built over it - everything
/// <see cref="BsdataNameResolver"/>/<see cref="BsdataDatasheetMapper"/> need to resolve names and
/// build Datasheets, in one value. This is the expensive-to-build, per-starting-file-static unit
/// army-roster-enrichment's app-wide catalogue cache (ProbHammer.Web) keys its cache by - this type
/// itself has no caching behavior, it's just what gets cached.
/// </summary>
public sealed class ResolvedBsdataCatalogue(
    BsdataClosure closure,
    IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
    IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
    IReadOnlyDictionary<string, BsProfile> profileIdIndex)
{
    public BsdataClosure Closure { get; } = closure;
    public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
    public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
    public IReadOnlyDictionary<string, BsProfile> ProfileIdIndex { get; } = profileIdIndex;

    public static ResolvedBsdataCatalogue Build(IBsdataCatalogueSource source, string startingFileName)
    {
        var closure = BsdataClosureResolver.Resolve(source, startingFileName);
        return new ResolvedBsdataCatalogue(
            closure,
            BsdataNameResolver.BuildIdIndex(closure),
            BsdataNameResolver.BuildGroupIdIndex(closure),
            BsdataNameResolver.BuildProfileIdIndex(closure));
    }

    public Datasheet ResolveDatasheet(string entryName)
    {
        var entry = BsdataNameResolver.Resolve(Closure, entryName)
            ?? throw new BsdataNameResolutionException(entryName, BuildFailureMessage(entryName));

        return BsdataDatasheetMapper.BuildDatasheet(entry, IdIndex, GroupIdIndex, ProfileIdIndex, Closure.GameSystem?.ForceEntries);
    }

    private string BuildFailureMessage(string entryName)
    {
        var suggestion = BsdataNameSuggestion.FindClosest(entryName, BsdataNameResolver.AllNames(Closure));
        return suggestion is null
            ? $"No catalogue entry named '{entryName}' was found."
            : $"No catalogue entry named '{entryName}' was found. Did you mean '{suggestion}'?";
    }
}

/// <summary>Thrown when a name (unit or weapon) fails to resolve against a
/// <see cref="ResolvedBsdataCatalogue"/> - see army-roster-enrichment's "Unit and Weapon Name
/// Resolution" requirement. Carries the offending text as a property, mirroring
/// <see cref="AmbiguousCharacteristicException"/>'s existing convention.</summary>
public sealed class BsdataNameResolutionException(string text, string message) : Exception(message)
{
    public string Text { get; } = text;
}
