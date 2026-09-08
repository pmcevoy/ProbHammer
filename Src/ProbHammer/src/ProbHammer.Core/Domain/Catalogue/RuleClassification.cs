namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The result of classifying a rule/ability's Name+Text (see
/// <see cref="RuleEffectClassifier"/>) - who it affects and what unconditional characteristic
/// mutations it states. <see cref="Effects"/> defaults to empty, never null - text stating no
/// recognizable effect classifies to zero Effects, not an absent list.</summary>
public sealed record RuleClassification(RuleTarget Target, IReadOnlyList<CharacteristicEffect> Effects)
{
    public RuleClassification(RuleTarget target) : this(target, [])
    {
    }
}