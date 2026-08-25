using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;

namespace ProbHammer.Core.Domain.Import.BattleScribe;

/// <summary>
/// Builds a roster-scoped <see cref="RuleGlossary"/> (see battlescribe-roster-import's Core Rule
/// Extraction requirement and design.md's "Roster-scoped RuleGlossary, reusing the existing type")
/// by walking the whole roster once, collecting every distinct rule entry (by id, first occurrence
/// wins) found anywhere - the force's own <c>rules</c>, every top-level selection's own
/// <c>rules</c>, and every nested wargear selection's own <c>rules</c> (e.g. a weapon's "Sustained
/// Hits"/"Anti" keyword rule text) - into <see cref="RuleDefinition"/>s. This is what gives
/// `/LivePlay`'s existing `[BRACKET]` cross-reference resolution and weapon-keyword-chip popovers
/// (rules-glossary-popovers) working text for a BattleScribe-sourced roster too, with zero changes
/// to that existing rendering pipeline - it only ever needs *a* RuleGlossary, not specifically a
/// BSData-sourced one.
/// </summary>
public static class BattleScribeRuleGlossaryBuilder
{
    public static RuleGlossary Build(BsRoster roster)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var definitions = new List<RuleDefinition>();

        void Collect(IEnumerable<BsRosterRule> rules)
        {
            foreach (var rule in rules)
            {
                if (seenIds.Add(rule.Id))
                    definitions.Add(new RuleDefinition(rule.Name, [], rule.Description, []));
            }
        }

        void Walk(BsRosterSelection selection)
        {
            Collect(selection.Rules);
            foreach (var child in selection.Selections)
                Walk(child);
        }

        foreach (var force in roster.Forces)
        {
            Collect(force.Rules);
            foreach (var selection in force.Selections)
                Walk(selection);
        }

        return RuleGlossary.BuildFrom(definitions);
    }
}
