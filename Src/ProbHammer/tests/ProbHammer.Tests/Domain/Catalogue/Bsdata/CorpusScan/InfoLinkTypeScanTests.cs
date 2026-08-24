using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone's `infoLink`
/// `type` values - see resolve-core-rule-abilities' design.md "Corpus scan scope" decision.
/// `infoLinks[].type` is the one place a genuinely new value can appear in real data and be
/// *silently* dropped, exactly the way `"rule"` was before this change (`BsdataDatasheetMapper
/// .WalkInfoLink` only recognizes an allowlisted set of types; every other type is a silent
/// no-op, by design - see WalkInfoLink's own trailing comment). This scan walks each catalogue
/// file's own closure directly (the same pattern `WeaponKeywordScanTests` already uses), not
/// through `BuildDatasheet`, since `BuildDatasheet` has no way to report "I saw this infoLink and
/// did nothing with it."
/// </summary>
public class InfoLinkTypeScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_infoLink_type_scan()
    {
        var source = LiveClone.RequireSource();
        var occurrencesByType = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var ctx = new WalkContext(
                BsdataNameResolver.BuildIdIndex(closure),
                BsdataNameResolver.BuildGroupIdIndex(closure),
                fileName,
                occurrencesByType);

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
                WalkEntry(entry, ctx);
        }

        var uniqueTypes = occurrencesByType.Keys.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

        AllowlistCheck.AssertClean(
            uniqueTypes,
            InfoLinkTypeAllowlist.Entries,
            type => Describe(type, occurrencesByType[type]));
    }

    private static string Describe(string type, List<string> occurrences)
    {
        const int maxShown = 5;
        var suffix = occurrences.Count > maxShown ? $", +{occurrences.Count - maxShown} more" : "";
        return $"'{type}' (seen on: {string.Join(", ", occurrences.Take(maxShown))}{suffix})";
    }

    private sealed class WalkContext(
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        string fileName,
        Dictionary<string, List<string>> occurrencesByType)
    {
        public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
        public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
        public string FileName { get; } = fileName;
        public Dictionary<string, List<string>> OccurrencesByType { get; } = occurrencesByType;
        public HashSet<string> VisitedEntryIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedGroupIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static void WalkEntry(BsSelectionEntry entry, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(entry.Id) && !ctx.VisitedEntryIds.Add(entry.Id))
            return;

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, ctx);

        foreach (var link in entry.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in entry.InfoLinks)
            RecordInfoLink(link, ctx);
    }

    private static void WalkGroup(BsSelectionEntryGroup group, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(group.Id) && !ctx.VisitedGroupIds.Add(group.Id))
            return;

        foreach (var child in group.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var nested in group.SelectionEntryGroups)
            WalkGroup(nested, ctx);

        foreach (var link in group.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in group.InfoLinks)
            RecordInfoLink(link, ctx);
    }

    private static void WalkLink(BsEntryLink link, WalkContext ctx)
    {
        if (ctx.IdIndex.TryGetValue(link.TargetId, out var targetEntry))
            WalkEntry(targetEntry, ctx);
        else if (ctx.GroupIdIndex.TryGetValue(link.TargetId, out var targetGroup))
            WalkGroup(targetGroup, ctx);
    }

    private static void RecordInfoLink(BsEntryLink link, WalkContext ctx)
    {
        if (!ctx.OccurrencesByType.TryGetValue(link.Type, out var occurrences))
            ctx.OccurrencesByType[link.Type] = occurrences = [];

        occurrences.Add($"{ctx.FileName} :: '{link.Name}'");
    }
}
