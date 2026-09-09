using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed <see cref="ScalarCharacteristicEffect"/>/
/// <see cref="InvulnerableSaveCharacteristicEffect"/> subtypes - a rule/ability's text states an
/// unconditional mutation of exactly one named characteristic, and that mutation's own shape (a
/// plain Statline scalar's Verb+Amount vs. InSv's compound melee/ranged pair) genuinely varies per
/// characteristic, mirroring <see cref="RuleTarget"/>'s/<see cref="WeaponProfile"/>'s own
/// abstract-base/sealed-subtype convention rather than a single record trying to represent both
/// shapes with unused fields. <see cref="RuleClassification.Effects"/> stays a single, uniformly
/// -typed list across both subtypes - see resolve-invulnerable-save-effects/design.md's D1 for why
/// InSv joins this list as a new element type rather than a separate top-level field. The
/// <see cref="JsonPolymorphicAttribute"/>/<see cref="JsonDerivedTypeAttribute"/> pair mirrors
/// <see cref="RuleTarget"/>'s own convention exactly - see design.md's D7.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarCharacteristicEffect), "Scalar")]
[JsonDerivedType(typeof(InvulnerableSaveCharacteristicEffect), "InvulnerableSave")]
public abstract record CharacteristicEffect;

/// <summary>One atomic, unconditional mutation a rule/ability's text states against exactly one
/// named <see cref="Statline"/> scalar characteristic. <see cref="Characteristic"/> is a plain
/// string matching Statline's own scalar property names ("M"/"T"/"Sv"/"W"/"Ld"/"Oc"), the same
/// convention <see cref="CharacteristicModifierCandidate"/> already uses - not a new enum.
/// <see cref="Amount"/> is the text's own stated value, unsigned/unresolved - see
/// <see cref="EffectVerb"/>'s own doc comment for why resolving a final signed value is out of
/// scope. Today's original single-shape <c>CharacteristicEffect</c> record, renamed and retyped as
/// a sealed subtype by resolve-invulnerable-save-effects (see design.md's D1) once InSv needed a
/// second, differently-shaped sibling.</summary>
public sealed record ScalarCharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount)
    : CharacteristicEffect;

/// <summary>An unconditional invulnerable-save grant a rule/ability's text states, as a melee/ranged
/// pair - always Set-shaped (see resolve-invulnerable-save-effects/design.md's D2: every real corpus
/// grant on this axis is a flat grant, never an Improve/Worsen), with an attack type the text
/// doesn't name for this grant represented as <c>0</c> on that side (D3 - reuses
/// <see cref="InvulnerableSave.None"/>'s own sentinel convention; no real 11e invulnerable save is
/// ever stated as "0+"/"1+").</summary>
public sealed record InvulnerableSaveCharacteristicEffect(InvulnerableSave Value) : CharacteristicEffect;