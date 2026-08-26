using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone - see
/// bsdata-corpus-scan's Full-Corpus Army-Rule Name Resolution Scan. Every entry in
/// <see cref="ArmyRuleNameLookup"/>'s curated inclusion table is resolved against its own
/// faction's real catalogue closure in the clone, mitigating design.md's "mid-edition codex/BSData
/// renames silently break a table entry" risk - a rename or removal shows up here as an
/// unresolved name rather than silently keeping a stale table entry.
/// </summary>
public class ArmyRuleNameResolutionScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_army_rule_name_resolution_scan()
    {
        var source = LiveClone.RequireSource();
        var availableFileNames = source.ListFileNames();

        var failures = new List<(string Faction, string Name)>();

        foreach (var (faction, names) in ArmyRuleNameLookup.Entries)
        {
            var startingFileName = BsdataFactionResolver.ResolveStartingFileName([faction], availableFileNames);
            var closure = BsdataClosureResolver.Resolve(source, startingFileName);
            var glossary = RuleGlossary.Build(closure);

            foreach (var name in names)
            {
                if (glossary.TryResolve(name) is null)
                    failures.Add((faction, name));
            }
        }

        AllowlistCheck.AssertClean(failures, ArmyRuleNameResolutionAllowlist.Entries,
            f => $"{f.Faction} :: '{f.Name}'");
    }
}
