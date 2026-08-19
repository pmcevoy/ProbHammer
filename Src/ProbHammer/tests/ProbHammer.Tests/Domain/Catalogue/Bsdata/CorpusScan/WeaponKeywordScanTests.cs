using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone's weapon
/// "Keywords" characteristics - see design.md. Walks each catalogue file's own closure directly
/// (not through <see cref="BsdataDatasheetMapper.BuildDatasheet"/>, which has no way to enumerate
/// every weapon profile it resolves - see Datasheet.ResolveWeaponProfile's deliberate
/// on-demand-only shape) so an unrelated characteristic-resolution failure elsewhere on the same
/// entry never blocks this scan from seeing that entry's weapons.
/// </summary>
public class WeaponKeywordScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_weapon_keyword_token_scan()
    {
        var source = LiveClone.RequireSource();
        var occurrencesByToken = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var ctx = new WalkContext(
                BsdataNameResolver.BuildIdIndex(closure),
                BsdataNameResolver.BuildGroupIdIndex(closure),
                BsdataNameResolver.BuildProfileIdIndex(closure),
                fileName,
                occurrencesByToken);

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
                WalkEntry(entry, ctx);
        }

        var uniqueTokens = occurrencesByToken.Keys.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

        AllowlistCheck.AssertClean(
            uniqueTokens,
            WeaponKeywordAllowlist.Entries,
            token => Describe(token, occurrencesByToken[token]));
    }

    private static string Describe(string token, List<string> occurrences)
    {
        const int maxShown = 5;
        var suffix = occurrences.Count > maxShown ? $", +{occurrences.Count - maxShown} more" : "";
        return $"'{token}' (seen on: {string.Join(", ", occurrences.Take(maxShown))}{suffix})";
    }

    private sealed class WalkContext(
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        IReadOnlyDictionary<string, BsProfile> profileIdIndex,
        string fileName,
        Dictionary<string, List<string>> occurrencesByToken)
    {
        public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
        public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
        public IReadOnlyDictionary<string, BsProfile> ProfileIdIndex { get; } = profileIdIndex;
        public string FileName { get; } = fileName;
        public Dictionary<string, List<string>> OccurrencesByToken { get; } = occurrencesByToken;
        public HashSet<string> VisitedEntryIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedGroupIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static void WalkEntry(BsSelectionEntry entry, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(entry.Id) && !ctx.VisitedEntryIds.Add(entry.Id))
            return;

        foreach (var profile in entry.Profiles)
            CollectWeaponTokens(profile, ctx);

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, ctx);

        foreach (var link in entry.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in entry.InfoLinks)
            WalkInfoLink(link, ctx);
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
            WalkInfoLink(link, ctx);
    }

    private static void WalkLink(BsEntryLink link, WalkContext ctx)
    {
        if (ctx.IdIndex.TryGetValue(link.TargetId, out var targetEntry))
            WalkEntry(targetEntry, ctx);
        else if (ctx.GroupIdIndex.TryGetValue(link.TargetId, out var targetGroup))
            WalkGroup(targetGroup, ctx);
    }

    private static void WalkInfoLink(BsEntryLink link, WalkContext ctx)
    {
        if (link.Type == "profile" && ctx.ProfileIdIndex.TryGetValue(link.TargetId, out var profile))
            CollectWeaponTokens(profile, ctx);
    }

    private static void CollectWeaponTokens(BsProfile profile, WalkContext ctx)
    {
        if (profile.TypeName != "Ranged Weapons" && profile.TypeName != "Melee Weapons")
            return;

        var keywordsText = profile.CharacteristicText("Keywords") ?? "-";
        foreach (var token in WeaponKeywordParser.UnrecognizedTokens(keywordsText))
        {
            if (!ctx.OccurrencesByToken.TryGetValue(token, out var occurrences))
                ctx.OccurrencesByToken[token] = occurrences = [];

            occurrences.Add($"{ctx.FileName} :: '{profile.Name}'");
        }
    }
}
