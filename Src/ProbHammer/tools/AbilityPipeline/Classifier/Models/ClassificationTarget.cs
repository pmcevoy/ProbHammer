using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Standalone mirror of <c>ProbHammer.Core.Domain.Catalogue.RuleTarget</c>'s own
/// discriminated union - see design.md's "Stage 2 does NOT reference ProbHammer.Core" decision for
/// why this is a deliberate duplicate, not a shared type, and Principle #4 ("Target and Effect stay
/// independent axes") for why this exists as its own field rather than folded into an Effect.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(SelfTarget), "Self")]
[JsonDerivedType(typeof(AttachedUnitTarget), "AttachedUnit")]
[JsonDerivedType(typeof(KeywordTarget), "Keyword")]
[JsonDerivedType(typeof(UnconditionalTarget), "Unconditional")]
public abstract record ClassificationTarget;

/// <summary>The rule's own bearer alone, no broader target language recognized. The default/
/// fallback classification, not the absence of a result.</summary>
public sealed record SelfTarget : ClassificationTarget;

/// <summary>The bearer's whole attached unit (every component of an AttachedUnit), e.g. Vexilla's
/// "models in the bearer's unit".</summary>
public sealed record AttachedUnitTarget : ClassificationTarget;

/// <summary>A roster-wide target filtered by a named keyword found in the text (e.g. Templar Vows'
/// "ADEPTUS ASTARTES").</summary>
public sealed record KeywordTarget(string Keyword) : ClassificationTarget;

/// <summary>A roster-wide target with no keyword qualifier.</summary>
public sealed record UnconditionalTarget : ClassificationTarget;
