using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Fixtures;

/// <summary>A small, hand-built <see cref="RuleClassificationBaseline"/> reproducing exactly the two
/// real baseline entries (Shield Dome, Vexilla) apply-rule-effect-baseline retired
/// <c>StatlineFlagRuleCatalogue</c> in favor of - mirrors the BSData trimmed-fixture testing
/// convention (a small, real excerpt, not the full checked-in corpus file). Text/Target/Effects
/// copied verbatim from src/ProbHammer.Web/Data/RuleEffectClassifications.json.</summary>
public static class RuleClassificationBaselineFixtures
{
    public static readonly RuleClassificationBaseline ShieldDomeAndVexilla = RuleClassificationBaseline.FromEntries(
    [
        new RuleClassificationBaselineEntry(
            Text: "The bearer has a 5+ invulnerable save.",
            Target: new SelfRuleTarget(),
            Effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5))]),
        new RuleClassificationBaselineEntry(
            Text: "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
            Target: new AttachedUnitRuleTarget(),
            Effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
    ]);
}
