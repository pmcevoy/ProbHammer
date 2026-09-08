using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed <see cref="SelfRuleTarget"/>/<see cref="AttachedUnitRuleTarget"/>/
/// <see cref="KeywordRuleTarget"/>/<see cref="UnconditionalRuleTarget"/> subtypes, never null - who a
/// rule/ability's text affects is always exactly one of these, mirroring <see cref="WeaponProfile"/>'s/
/// <see cref="CharacteristicValue"/>'s own abstract-base/sealed-subtype convention.
/// <see cref="SelfRuleTarget"/> is the classifier's explicit default/fallback result, not the absence
/// of a result. See classify-rule-effects-from-text/design.md's "RuleTarget is an abstract base with
/// sealed subtypes, never nullable" and "Self and AttachedUnit are separate cases, not merged"
/// decisions - the latter exists because a <c>DetachmentRule</c> carries no <see cref="Ability.Scope"/>
/// field to defer to. The <see cref="JsonPolymorphicAttribute"/>/<see cref="JsonDerivedTypeAttribute"/>
/// pair mirrors <c>StoredArmyImport</c>'s own polymorphic-record convention - added for
/// baseline-rule-effect-classifications, whose checked-in baseline JSON serializes a
/// <c>RuleClassification</c>'s own <see cref="RuleTarget"/> directly rather than a parallel
/// representation (see that change's design.md "Baseline file shape" decision).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SelfRuleTarget), "Self")]
[JsonDerivedType(typeof(AttachedUnitRuleTarget), "AttachedUnit")]
[JsonDerivedType(typeof(KeywordRuleTarget), "Keyword")]
[JsonDerivedType(typeof(UnconditionalRuleTarget), "Unconditional")]
public abstract record RuleTarget;

/// <summary>The rule's own bearer alone, no broader target language recognized.</summary>
public sealed record SelfRuleTarget : RuleTarget;

/// <summary>The bearer's whole attached unit (every component of an <c>AttachedUnit</c>), e.g.
/// Vexilla's "models in the bearer's unit".</summary>
public sealed record AttachedUnitRuleTarget : RuleTarget;

/// <summary>A roster-wide target filtered by a named keyword found in the text (e.g. Templar Vows'
/// "ADEPTUS ASTARTES", a Detachment rule's "SWORD BRETHREN SQUAD"). Evaluating this predicate against
/// an actual resolved roster is out of scope for this change - see design.md's Non-Goals.</summary>
public sealed record KeywordRuleTarget(string Keyword) : RuleTarget;

/// <summary>A roster-wide target with no keyword qualifier. Kept for completeness; not required to be
/// exercised by a real example - see design.md's Open Questions.</summary>
public sealed record UnconditionalRuleTarget : RuleTarget;