using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>One real BsModifier occurrence found targeting a Statline characteristic field
/// (including the deliberately-excluded InSv id - see CharacteristicModifierClassificationAllowlist)
/// - only unclassified occurrences are ever added to the scan's own results list (see
/// Full_corpus_characteristic_modifier_classification_scan's own doc comment for why).</summary>
public sealed record ModifierOccurrence(string File, string EntryName, string Field, bool HasCondition)
{
    public override string ToString() =>
        $"{File} :: '{EntryName}' (field={Field}, hasCondition={HasCondition})";
}

/// <summary>
/// Permanent, manually-triggered regression scan (same [Fact(Explicit = true)] pattern as the other
/// CorpusScan tests) proving characteristic-modifier-caveats' classifier's real-corpus behavior:
/// every entry anywhere in the live clone carrying a modifier that targets a Statline characteristic
/// field (the 6-field allowlist, plus InSv - deliberately excluded, see the allowlist's own doc
/// comment) is either successfully classified by the real production classifier, or unclassified for
/// one of the documented reasons in CharacteristicModifierClassificationAllowlist.
///
/// Reuses the real, public BsdataDatasheetMapper.BuildDatasheet as the classification oracle rather
/// than re-deriving a second copy of the classifier's own tier-1/tier-2 predicate here (which would
/// let this scan and the classifier share the same bug undetected) - each interesting entry is
/// re-rooted as if it were its own Datasheet's starting entry (BuildDatasheet has no way to inspect
/// an already-built Datasheet's own classification input, only its output, so this is the only way
/// to drive the real classifier against one specific entry directly). Only UNCLASSIFIED occurrences
/// are collected into the allowlist-checked results list, mirroring
/// CharacteristicResolutionScanTests' own "only collect the failures" convention - a successfully
/// classified occurrence needs no explanation. A separate sanity assertion confirms the scan
/// actually found real tier-1 classifications too (never silently zero).
/// </summary>
public class CharacteristicModifierClassificationScanTests
{
    private static readonly HashSet<string> AuditFieldIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "e703-ecb6-5ce7-aec1", // M
        "d29d-cf75-fc2d-34a4", // T
        "450-a17e-9d5e-29da", // Sv
        "750a-a2ec-90d3-21fe", // W
        "58d2-b879-49c7-43bc", // Ld
        "bef7-942a-1a23-59f8", // Oc
        CharacteristicModifierClassificationAllowlist.InvulnerableSaveFieldId
    };

    [Fact(Explicit = true)]
    public void Full_corpus_characteristic_modifier_classification_scan()
    {
        var source = LiveClone.RequireSource();
        var unclassified = new List<ModifierOccurrence>();
        var classifiedCount = 0;

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);
            var ctx = new WalkContext(idIndex, groupIndex, fileName);

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
                WalkEntry(entry, ctx);

            foreach (var (entry, modifier) in ctx.InterestingEntries)
            {
                var sheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
                var wasClassified = sheet.CharacteristicModifierCandidates.Any(c => c.EntryName == entry.Name);

                if (wasClassified)
                {
                    classifiedCount++;
                    continue;
                }

                var hasCondition = modifier.Conditions.Count > 0 || modifier.ConditionGroups.Count > 0;
                unclassified.Add(new ModifierOccurrence(fileName, entry.Name, modifier.Field, hasCondition));
            }
        }

        Assert.True(classifiedCount > 0,
            "Expected at least one real corpus modifier to classify successfully (e.g. a tier-1 " +
            "Enhancement) - zero would mean this scan (or the classifier itself) silently stopped " +
            "finding anything real.");

        AllowlistCheck.AssertClean(unclassified, CharacteristicModifierClassificationAllowlist.Entries,
            o => o.ToString());
    }

    private sealed class WalkContext(
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        string fileName)
    {
        public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
        public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
        public string FileName { get; } = fileName;
        public List<(BsSelectionEntry Entry, BsModifier Modifier)> InterestingEntries { get; } = [];
        public HashSet<string> VisitedEntryIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedGroupIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    // Entries only, mirroring BsdataDatasheetMapper's own classifier scope (never a group's own
    // Modifiers - see ClassifyCharacteristicModifierCandidates' own doc comment for why).
    private static void WalkEntry(BsSelectionEntry entry, WalkContext ctx)
    {
        if (!string.IsNullOrEmpty(entry.Id) && !ctx.VisitedEntryIds.Add(entry.Id))
            return;

        foreach (var modifier in entry.Modifiers)
        {
            if (AuditFieldIds.Contains(modifier.Field))
                ctx.InterestingEntries.Add((entry, modifier));
        }

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, ctx);

        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, ctx);

        foreach (var link in entry.EntryLinks)
            WalkLink(link, ctx);
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
    }

    private static void WalkLink(BsEntryLink link, WalkContext ctx)
    {
        if (ctx.IdIndex.TryGetValue(link.TargetId, out var targetEntry))
            WalkEntry(targetEntry, ctx);
        else if (ctx.GroupIdIndex.TryGetValue(link.TargetId, out var targetGroup))
            WalkGroup(targetGroup, ctx);
    }
}
