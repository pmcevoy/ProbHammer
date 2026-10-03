using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Who a rule/ability's text affects - always exactly one of these subtypes, never null.
/// Self and AttachedUnit are separate cases because a <c>DetachmentRule</c> carries no
/// <see cref="Ability.Scope"/> field to defer to.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SelfRuleTarget), "Self")]
[JsonDerivedType(typeof(AttachedUnitRuleTarget), "AttachedUnit")]
[JsonDerivedType(typeof(KeywordRuleTarget), "Keyword")]
[JsonDerivedType(typeof(UnconditionalRuleTarget), "Unconditional")]
public abstract record RuleTarget;

/// <summary>The rule's own bearer alone, e.g. "the bearer"/"this model".</summary>
public sealed record SelfRuleTarget : RuleTarget;

/// <summary>The bearer's whole attached unit (every component of an <c>AttachedUnit</c>), e.g.
/// Vexilla's "models in the bearer's unit".</summary>
public sealed record AttachedUnitRuleTarget : RuleTarget;

/// <summary>Every unit carrying all of <see cref="Keywords"/>, e.g. "a friendly Leagues of Votann
/// Infantry unit" is LEAGUES OF VOTANN and INFANTRY.</summary>
public sealed record KeywordRuleTarget(IReadOnlyList<string> Keywords) : RuleTarget
{
    public bool Equals(KeywordRuleTarget? other) =>
        other is not null && Keywords.SequenceEqual(other.Keywords);

    public override int GetHashCode() =>
        Keywords.Aggregate(0, (hash, keyword) => HashCode.Combine(hash, keyword));
}

/// <summary>A roster-wide target with no keyword qualifier.</summary>
public sealed record UnconditionalRuleTarget : RuleTarget;
