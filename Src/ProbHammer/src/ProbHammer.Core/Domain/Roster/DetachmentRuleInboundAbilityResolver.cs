using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>Matches every selected Detachment's own rule text against the resolved roster's units,
/// populating each matched <see cref="ICombatUnit.InboundAbilities"/> - see
/// `army-roster-enrichment`'s Detachment Rule Keyword Target Resolution requirement. Operates purely
/// on already-resolved <see cref="Unit"/>/<see cref="ResolvedDetachment"/> domain objects, so both
/// import pipelines share one implementation.</summary>
public static class DetachmentRuleInboundAbilityResolver
{
    /// <summary>For each <see cref="DetachmentRule"/> across every <paramref name="detachments"/>
    /// entry, looks up its classification. A rule with none, or whose target is not a
    /// <see cref="KeywordRuleTarget"/>, attaches nothing. Otherwise appends one synthesized
    /// <see cref="Ability"/> (Name/Text verbatim from the <see cref="DetachmentRule"/>, Scope Unit,
    /// Origin Detachment Rule) to every unit in <paramref name="units"/> whose
    /// <see cref="KeywordResolution.EffectiveKeywords"/> contains every target keyword.</summary>
    public static void Apply(
        IReadOnlyList<ICombatUnit> units,
        IReadOnlyList<ResolvedDetachment> detachments,
        AbilityClassificationCatalogue classifications)
    {
        foreach (var rule in detachments.SelectMany(d => d.Rules))
        {
            if (!classifications.TryGet(rule.Text, out var classification))
                continue;

            if (classification.Target is not KeywordRuleTarget keywordTarget)
                continue;

            var ability = new Ability
            {
                Name = rule.Name,
                Text = rule.Text,
                Scope = AbilityScope.Unit,
                Origin = AbilityOrigin.DetachmentRule
            };

            foreach (var unit in units)
            {
                var unitKeywords = KeywordResolution.EffectiveKeywords(unit);
                if (!keywordTarget.Keywords.All(unitKeywords.Contains))
                    continue;

                unit.InboundAbilities = [.. unit.InboundAbilities, ability];
            }
        }
    }
}
