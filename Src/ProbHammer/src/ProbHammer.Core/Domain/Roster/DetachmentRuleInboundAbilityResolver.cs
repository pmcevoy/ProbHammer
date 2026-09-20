using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>Matches every selected Detachment's own rule text against the resolved roster's units,
/// populating each matched <see cref="ICombatUnit.InboundAbilities"/> - see
/// `army-roster-enrichment`'s Detachment Rule Keyword Target Resolution requirement. Independent of
/// <c>Domain.Catalogue.Bsdata</c> (same precedent as <c>ArmyRuleNameLookup</c>/
/// <c>InvulnerableSaveCaveatClassifier</c>): operates purely on already-resolved <see cref="Unit"/>/
/// <see cref="ResolvedDetachment"/> domain objects, so both import pipelines share one
/// implementation. Called once, after either pipeline's <see cref="ArmyRoster"/> is built (design.md
/// D5) - never a live <see cref="RuleEffectClassifier.Classify"/> call, only a checked-in
/// <see cref="RuleClassificationBaseline"/> lookup, mirroring
/// <c>AttachedUnitAggregator.ApplyStatlineFlagRules</c>'s own lookup convention.</summary>
public static class DetachmentRuleInboundAbilityResolver
{
    /// <summary>For each <see cref="DetachmentRule"/> across every <paramref name="detachments"/>
    /// entry, looks up its normalized Text against <paramref name="baseline"/>. A rule with no
    /// baseline entry, or whose matched Target is not a <see cref="KeywordRuleTarget"/>, attaches
    /// nothing (design.md D3 - <see cref="SelfRuleTarget"/>/<see cref="UnconditionalRuleTarget"/>
    /// deliberately produce no match here). Otherwise appends one synthesized <see cref="Ability"/>
    /// (Name/Text verbatim from the <see cref="DetachmentRule"/>, Scope Unit, Origin Detachment
    /// Rule) to every unit in <paramref name="units"/> whose
    /// <see cref="KeywordResolution.EffectiveKeywords"/> contains that keyword.</summary>
    public static void Apply(
        IReadOnlyList<ICombatUnit> units,
        IReadOnlyList<ResolvedDetachment> detachments,
        RuleClassificationBaseline baseline)
    {
        foreach (var rule in detachments.SelectMany(d => d.Rules))
        {
            var normalizedText = RuleEffectClassifier.Normalize(rule.Text);
            if (!baseline.TryGet(normalizedText, out var entry))
                continue;

            if (entry.Target is not KeywordRuleTarget keywordTarget)
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
                if (!KeywordResolution.EffectiveKeywords(unit).Contains(keywordTarget.Keyword))
                    continue;

                unit.InboundAbilities = [.. unit.InboundAbilities, ability];
            }
        }
    }
}
