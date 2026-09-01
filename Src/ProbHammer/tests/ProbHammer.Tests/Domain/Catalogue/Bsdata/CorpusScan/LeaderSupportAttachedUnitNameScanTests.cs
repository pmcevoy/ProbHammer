using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan (resolve-known-ability-effects) confirming no real
/// ability/rule in the live BSData clone shares the exact names "Leader"/"Support"/"Attached Unit"
/// with different intent than the attachment-eligibility restatement
/// datasheet-catalogue's "Attachment-Eligibility Abilities Are Excluded" assumes. Walks each starting
/// catalogue's own entry tree directly (local Abilities profiles, local Rules, and "rule"-type
/// InfoLinks resolved via the closure's own RuleGlossary) rather than going through
/// BsdataDatasheetMapper.BuildDatasheet, since that already applies the exclusion this scan needs to
/// audit - it would filter every occurrence away before this scan ever saw it.
///
/// Unlike this project's other corpus scans, results here (849 on the first run - one per real
/// Character/Leader-eligible datasheet in the corpus) are far too numerous to allowlist individually;
/// every real occurrence's text is instead checked against a single shared semantic signal
/// ("attach", case-insensitive) rather than the per-item AllowlistEntry/AllowlistCheck pattern - the
/// first run found this holds for all 849 with zero exceptions, so any future occurrence whose text
/// doesn't mention "attach" is the actual signal worth investigating.
/// </summary>
public class LeaderSupportAttachedUnitNameScanTests
{
    private static readonly HashSet<string> Names =
        new(StringComparer.OrdinalIgnoreCase) { "Leader", "Support", "Attached Unit" };

    public sealed record NamedOccurrence(string FileName, string EntryName, string AbilityName, string Text);

    [Fact(Explicit = true)]
    public void Full_corpus_leader_support_attached_unit_name_scan()
    {
        var source = LiveClone.RequireSource();
        var results = new List<NamedOccurrence>();

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var glossary = RuleGlossary.Build(closure);
            var startingCatalogue = closure.Files[0].Catalogue;

            foreach (var entry in startingCatalogue.SharedSelectionEntries)
                WalkEntry(entry, fileName, glossary, results);
        }

        var differentIntent =
            results.Where(r => !r.Text.Contains("attach", StringComparison.OrdinalIgnoreCase)).ToList();
        if (differentIntent.Count > 0)
            Assert.Fail(string.Join("\n",
                new[] { $"{differentIntent.Count} occurrence(s) don't mention attachment eligibility:" }
                    .Concat(differentIntent.Select(r =>
                        $"  - {r.FileName} :: '{r.EntryName}' / '{r.AbilityName}' -> \"{r.Text}\""))));
    }

    private static void WalkEntry(BsSelectionEntry entry, string fileName, RuleGlossary glossary,
        List<NamedOccurrence> results)
    {
        foreach (var profile in entry.Profiles)
            if (profile.TypeName == "Abilities" && Names.Contains(profile.Name))
                results.Add(new NamedOccurrence(
                    fileName, entry.Name, profile.Name, profile.CharacteristicText("Description") ?? ""));

        foreach (var rule in entry.Rules)
            if (Names.Contains(rule.Name))
                results.Add(new NamedOccurrence(fileName, entry.Name, rule.Name, rule.Description));

        foreach (var link in entry.InfoLinks)
            if (link.Type == "rule" && Names.Contains(link.Name))
            {
                var rule = glossary.TryResolve(link.Name);
                if (rule is not null)
                    results.Add(new NamedOccurrence(fileName, entry.Name, link.Name, rule.Text));
            }

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, fileName, glossary, results);
        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, fileName, glossary, results);
    }

    private static void WalkGroup(BsSelectionEntryGroup group, string fileName, RuleGlossary glossary,
        List<NamedOccurrence> results)
    {
        foreach (var link in group.InfoLinks)
            if (link.Type == "rule" && Names.Contains(link.Name))
            {
                var rule = glossary.TryResolve(link.Name);
                if (rule is not null)
                    results.Add(new NamedOccurrence(fileName, group.Name, link.Name, rule.Text));
            }

        foreach (var child in group.SelectionEntries)
            WalkEntry(child, fileName, glossary, results);
        foreach (var childGroup in group.SelectionEntryGroups)
            WalkGroup(childGroup, fileName, glossary, results);
    }
}