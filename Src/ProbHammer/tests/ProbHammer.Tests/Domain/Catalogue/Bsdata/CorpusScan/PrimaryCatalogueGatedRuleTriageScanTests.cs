using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone - see
/// bsdata-corpus-scan's Full-Corpus Primary-Catalogue-Gated Rule Triage Scan. Collects every rule
/// definition anywhere in the clone (every real catalogue file's own local <c>rules</c>/
/// <c>sharedRules</c>, plus the game system's own <c>sharedRules</c>, deduped by id/name across
/// the whole run) carrying the same "primary-catalogue"-scoped gating shape
/// <c>BsdataDatasheetMapper</c> used to read directly for Origin classification (before
/// classify-known-army-rules removed that dependency - see design.md's "repurposed as a scan-time
/// discovery aid" decision) and requires each to be accounted for by either
/// <see cref="ArmyRuleNameLookup"/>'s curated inclusion table or its curated exclusion list -
/// failing on anything neither covers, so a future BSData/codex update's new shared-library rule
/// gets flagged for a human to triage rather than silently doing nothing or silently
/// misclassifying, the way the original structural-only signal did for Assigned Agents/Disparate
/// Paths/Corsairs and Travelling Players.
///
/// A gated name already in the inclusion table is filtered out before ever becoming a "result" -
/// not checked via the bidirectional allowlist below - because several inclusion-table entries
/// (Nurgle's Gift, Martial Ka'tah, Gate of Infinity, Cult Ambush) are genuinely ungated in the
/// real corpus (their own dedicated file, no sibling rule to distinguish from) and would
/// incorrectly read as a "stale" allowlist entry if they were ever expected to appear gated here.
/// Only the exclusion list is used as the actual bidirectional allowlist.
/// </summary>
public class PrimaryCatalogueGatedRuleTriageScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_primary_catalogue_gated_rule_triage_scan()
    {
        var source = LiveClone.RequireSource();
        var seen = new Dictionary<string, (BsRule Rule, string SourceFile)>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var startingCatalogue = closure.Files[0].Catalogue;

            foreach (var rule in startingCatalogue.Rules.Concat(startingCatalogue.SharedRules))
                seen.TryAdd(RuleKey(rule), (rule, fileName));

            if (closure.GameSystem is not null)
                foreach (var rule in closure.GameSystem.SharedRules)
                    seen.TryAdd(RuleKey(rule), (rule, "<game system>"));
        }

        var gated = seen.Values.Where(x => HasPrimaryCatalogueScope(x.Rule.Modifiers)).ToList();

        var untriaged = gated
            .Where(x => !ArmyRuleNameLookup.AllKnownNames.Contains(x.Rule.Name))
            .ToList();

        var exclusionAllowlist = ArmyRuleNameLookup.ExclusionList
            .Select(e => new AllowlistEntry<(BsRule Rule, string SourceFile)>(
                $"{e.Name} - {e.Reason}",
                x => string.Equals(x.Rule.Name, e.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        AllowlistCheck.AssertClean(untriaged, exclusionAllowlist, Describe);
    }

    private static string Describe((BsRule Rule, string SourceFile) x) => $"{x.SourceFile} :: '{x.Rule.Name}'";

    private static string RuleKey(BsRule rule) => string.IsNullOrEmpty(rule.Id) ? rule.Name : rule.Id;

    /// <summary>The exact same structural signal <c>BsdataDatasheetMapper</c>'s own
    /// (now-removed) <c>HasPrimaryCatalogueScope</c> used to compute for Origin classification -
    /// duplicated here rather than reused from production code, matching this project's existing
    /// convention of a corpus scan owning its own self-contained walk (see
    /// <c>WeaponKeywordScanTests</c>).</summary>
    private static bool HasPrimaryCatalogueScope(IReadOnlyList<BsModifier> modifiers)
    {
        bool HasScope(IReadOnlyList<BsCondition> conditions, IReadOnlyList<BsConditionGroup> groups) =>
            conditions.Any(c => c.Scope == "primary-catalogue")
            || groups.Any(g => HasScope(g.Conditions, g.ConditionGroups));

        return modifiers.Any(m => m.Field == "hidden" && HasScope(m.Conditions, m.ConditionGroups));
    }
}
