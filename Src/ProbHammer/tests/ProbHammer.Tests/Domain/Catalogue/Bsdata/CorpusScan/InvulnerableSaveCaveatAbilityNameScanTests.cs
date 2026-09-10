using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan mirroring LeaderSupportAttachedUnitNameScanTests:
/// confirms every real occurrence of either InSv-caveat-internal ability-name convention
/// ("*Invulnerable Save", or the digit-parameterized "Invulnerable Save ({N}+*)") in the live
/// BSData clone is genuinely BsdataDatasheetMapper.ResolveCaveatAbility's own internal mechanism,
/// not a distinct, independently-meaningful ability that happens to share the name - the existing
/// ExcludedAttachmentAbilityNames exclusion has its own equivalent scan; this one needs the same
/// confirmation before Datasheet excludes these two conventions from the general ability walk.
///
/// Walks each starting catalogue's own entry tree directly (local Abilities profiles only - both
/// real naming conventions are always locally-nested per BsdataDatasheetMapper.ResolveCaveatAbility's
/// own doc comment, never a "rule"-type InfoLink) rather than through BsdataDatasheetMapper
/// .BuildDatasheet, since that already applies the exclusion this scan needs to audit - it would
/// filter every occurrence away before this scan ever saw it. Every occurrence is checked against a
/// single shared semantic signal ("invulnerable save" in its own Description text) via the same
/// AllowlistEntry/AllowlistCheck pattern the other CorpusScan tests use - one confirmed real anomaly
/// (Orks' Makari) is allowlisted rather than special-cased.
/// </summary>
public class InvulnerableSaveCaveatAbilityNameScanTests
{
    private const string GenericName = "*Invulnerable Save";

    private static readonly Regex ParameterizedNamePattern =
        new(@"^Invulnerable Save \(\d+\+\*\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsCaveatInternalName(string name) =>
        string.Equals(name, GenericName, StringComparison.OrdinalIgnoreCase) ||
        ParameterizedNamePattern.IsMatch(name);

    public sealed record NamedOccurrence(string FileName, string EntryName, string AbilityName, string Text)
    {
        public override string ToString() => $"{FileName} :: '{EntryName}' / '{AbilityName}' -> \"{Text}\"";
    }

    private static readonly IReadOnlyList<AllowlistEntry<NamedOccurrence>> Allowlist =
    [
        new(
            "Orks' Makari - the confirmed real anomaly this project already documents (see " +
            "InvulnerableSaveCaveatResolutionAllowlist's own pre-existing entry): the linked " +
            "ability restricts re-rolling save rolls, not an attack-type split, so its own text " +
            "never mentions invulnerable save even though it's genuinely the digit-parameterized " +
            "caveat-internal ability this mechanism resolves by id.",
            (NamedOccurrence o) => o.FileName == "Orks.json" && o.EntryName == "Makari" &&
                                   o.AbilityName == "Invulnerable Save (2+*)")
    ];

    [Fact(Explicit = true)]
    public void Full_corpus_invulnerable_save_caveat_ability_name_scan()
    {
        var source = LiveClone.RequireSource();
        var results = new List<NamedOccurrence>();

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var startingCatalogue = closure.Files[0].Catalogue;

            foreach (var entry in startingCatalogue.SharedSelectionEntries)
                WalkEntry(entry, fileName, results);
        }

        Assert.True(results.Count > 0,
            "Expected at least one real corpus occurrence of either InSv-caveat-internal ability " +
            "name - zero would mean this scan (or the naming convention itself) silently stopped " +
            "finding anything real.");

        var differentIntent =
            results.Where(r => !r.Text.Contains("invulnerable save", StringComparison.OrdinalIgnoreCase)).ToList();

        AllowlistCheck.AssertClean(differentIntent, Allowlist, r => r.ToString());
    }

    private static void WalkEntry(BsSelectionEntry entry, string fileName, List<NamedOccurrence> results)
    {
        foreach (var profile in entry.Profiles)
            if (profile.TypeName == "Abilities" && IsCaveatInternalName(profile.Name))
                results.Add(new NamedOccurrence(
                    fileName, entry.Name, profile.Name, profile.CharacteristicText("Description") ?? ""));

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, fileName, results);
        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, fileName, results);
    }

    private static void WalkGroup(BsSelectionEntryGroup group, string fileName, List<NamedOccurrence> results)
    {
        foreach (var child in group.SelectionEntries)
            WalkEntry(child, fileName, results);
        foreach (var childGroup in group.SelectionEntryGroups)
            WalkGroup(childGroup, fileName, results);
    }
}