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

    /// <summary>Every top-level entry name in the closure - for diagnostic purposes only (a "did you
    /// mean...?" suggestion on a resolution failure via <see cref="BsdataNameSuggestion"/>), never
    /// for resolution itself.</summary>
    public static IEnumerable<string> AllNames(BsdataClosure closure) =>
        closure.Files.SelectMany(f => f.Catalogue.SharedSelectionEntries).Select(e => e.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>Locates the closure's own Detachment-choice group and returns its direct child
    /// selectionEntries - each one a real Detachment choice. A Detachment choice is a NESTED child
    /// of this group, never a top-level sharedSelectionEntry itself, so <see cref="Resolve"/> can
    /// never reach it directly - see army-roster-enrichment's Detachment Name Resolution
    /// requirement and design.md's "dedicated nested-name index, not a change to Resolve's existing
    /// contract" decision.
    ///
    /// Confirmed by a full-corpus scan (<c>DetachmentGroupNameScanTests</c>) to take TWO real,
    /// structurally different shapes - design.md's own risk ("unverified across the full corpus")
    /// turned out to be correct that an outlier existed, just far more common than a single outlier:
    ///   1. A top-level sharedSelectionEntryGroup named "Detachment" (or "Detachments" - both real,
    ///      confirmed singular on Space Marines, plural on the Aeldari Library) directly.
    ///   2. A top-level sharedSelectionEntries ENTRY of the same name (confirmed on Chaos Space
    ///      Marines, Necrons, Death Guard, Emperor's Children, Thousand Sons, World Eaters, Leagues
    ///      of Votann, Chaos Daemons via its Library file) wrapping ONE nested selectionEntryGroup -
    ///      itself named "Detachment"/"Detachments" when it carries the real choices directly, but a
    ///      handful of factions (confirmed: Tyranids, Genestealer Cults) instead wrap an EMPTY-named,
    ///      empty-entries group that only reaches the real choices via its own entryLink into a
    ///      "Library" catalogue file - unreachable today because that specific catalogueLink's own
    ///      <c>importRootEntries</c> is false in the source data (a genuine, confirmed corpus gap,
    ///      distinct from and out of scope for this method - see
    ///      DetachmentGroupNameAllowlist for the full list of factions this affects). Falling
    ///      through to the wrapper entry's sole nested group regardless of ITS OWN name is
    ///      deliberately NOT attempted as a further fallback: an empty-named, zero-entry group is
    ///      indistinguishable from "no real choices reachable in this closure" and returning it
    ///      would silently produce a Detachment list that's empty either way - the honest answer for
    ///      that gap is "no known Detachments" (surfacing as a loud resolution failure downstream),
    ///      not a guessed shape.
    /// Checked per file in closure order (local-over-imported precedence, mirroring
    /// <see cref="Resolve"/>'s own "first match wins" convention) - both shapes are tried within one
    /// file before moving to the next. Returns an empty list when neither shape is found anywhere in
    /// the closure.</summary>
    public static IReadOnlyList<BsSelectionEntry> ResolveDetachmentEntries(BsdataClosure closure)
    {
        foreach (var (_, catalogue) in closure.Files)
        {
            var directGroup = catalogue.SharedSelectionEntryGroups.FirstOrDefault(IsDetachmentGroupName);
            if (directGroup != null)
                return directGroup.SelectionEntries;

            var wrapperEntry = catalogue.SharedSelectionEntries.FirstOrDefault(IsDetachmentGroupName);
            var nestedGroup = wrapperEntry?.SelectionEntryGroups.FirstOrDefault(IsDetachmentGroupName);
            if (nestedGroup != null && nestedGroup.SelectionEntries.Count > 0)
                return nestedGroup.SelectionEntries;
        }

        return [];
    }

    private static bool IsDetachmentGroupName(BsSelectionEntryGroup group) => IsDetachmentGroupName(group.Name);
    private static bool IsDetachmentGroupName(BsSelectionEntry entry) => IsDetachmentGroupName(entry.Name);

    private static bool IsDetachmentGroupName(string name) =>
        string.Equals(name, "Detachment", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Detachments", StringComparison.OrdinalIgnoreCase);
}