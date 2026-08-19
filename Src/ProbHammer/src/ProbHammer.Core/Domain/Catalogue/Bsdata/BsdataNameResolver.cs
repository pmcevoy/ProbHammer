using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Resolves a unit name to its <see cref="BsSelectionEntry"/> over a closure, checking the
/// starting file's own sharedSelectionEntries first and only falling through to each imported
/// file (in closure order) on a miss - see design.md's "Local-Over-Imported Name Precedence".
/// </summary>
public static class BsdataNameResolver
{
    public static BsSelectionEntry? Resolve(BsdataClosure closure, string name)
    {
        foreach (var (_, catalogue) in closure.Files)
        {
            var match = catalogue.SharedSelectionEntries
                .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        return null;
    }

    /// <summary>
    /// Builds an id index over every top-level sharedSelectionEntry in the closure, nearer files'
    /// entries taking precedence on an (extremely unlikely) id collision. Used to resolve
    /// entryLinks encountered while walking a resolved entry's subtree - entryLinks carry a
    /// targetId, not a name, and that id may point into any file in the closure (see design.md's
    /// "entryLink target location" risk: never assume an entryLink's shape alone indicates
    /// local-vs-imported).
    /// </summary>
    public static IReadOnlyDictionary<string, BsSelectionEntry> BuildIdIndex(BsdataClosure closure)
    {
        var index = new Dictionary<string, BsSelectionEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, catalogue) in closure.Files)
        foreach (var entry in catalogue.SharedSelectionEntries)
            index.TryAdd(entry.Id, entry);

        return index;
    }

    /// <summary>Same idea as <see cref="BuildIdIndex"/>, but for top-level shared selection-entry
    /// groups - some entryLinks target a reusable wargear-option group rather than a single
    /// entry.</summary>
    public static IReadOnlyDictionary<string, BsSelectionEntryGroup> BuildGroupIdIndex(BsdataClosure closure)
    {
        var index = new Dictionary<string, BsSelectionEntryGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, catalogue) in closure.Files)
        foreach (var group in catalogue.SharedSelectionEntryGroups)
            index.TryAdd(group.Id, group);

        return index;
    }

    /// <summary>Indexes standalone profiles (BsCatalogue.SharedProfiles) by id across the closure,
    /// plus the closure's GameSystem (if resolved) - a selectionEntry's infoLinks can reference
    /// one of these directly rather than nesting the profile in its own Profiles list (see
    /// BsdataDatasheetMapper), and the base "Invulnerable Save (X+*)" abilities a footnoted
    /// invulnerable save links to live only in the game system's own SharedProfiles, not any
    /// catalogue's.</summary>
    public static IReadOnlyDictionary<string, BsProfile> BuildProfileIdIndex(BsdataClosure closure)
    {
        var index = new Dictionary<string, BsProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, catalogue) in closure.Files)
        foreach (var profile in catalogue.SharedProfiles)
            index.TryAdd(profile.Id, profile);

        if (closure.GameSystem is not null)
            foreach (var profile in closure.GameSystem.SharedProfiles)
                index.TryAdd(profile.Id, profile);

        return index;
    }
}
