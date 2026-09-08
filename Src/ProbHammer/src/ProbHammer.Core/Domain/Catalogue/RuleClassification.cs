namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The result of classifying a rule/ability's Name+Text (see
/// <see cref="RuleEffectClassifier"/>) - who it affects and what unconditional characteristic
/// mutations it states. <see cref="Effects"/> defaults to empty, never null - text stating no
/// recognizable effect classifies to zero Effects, not an absent list. <see cref="IsCaveated"/>
/// defaults to false - see widen-rule-effect-classification-coverage design.md's "Caveat signal
/// computed from raw match end positions" decision: true only when at least one Effect was
/// extracted AND real text remains, structurally, after the last regex match that contributed to
/// either Target or Effects. The default `false` is deliberately the "boring default" the
/// rule-effect-classification-baseline capability's own schema-growth backfill reads off this
/// type's own serialization - see <see cref="RuleClassificationDiff.DefaultClassification"/>.</summary>
public sealed record RuleClassification(
    RuleTarget Target,
    IReadOnlyList<CharacteristicEffect> Effects,
    bool IsCaveated = false)
{
    public RuleClassification(RuleTarget target) : this(target, [])
    {
    }
}