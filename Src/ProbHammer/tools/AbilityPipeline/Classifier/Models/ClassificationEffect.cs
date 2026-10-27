using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Every effect shape this pipeline's schema can state - the three existing
/// <c>CharacteristicEffect</c> shapes (Scalar/InvulnerableSave/Weapon-characteristic, mirrored
/// standalone) plus the three real corpus-confirmed gaps this change exists to capture: Feel No
/// Pain grants, weapon-scoped keyword grants, and named-ability grants. See
/// design.md's "New Effect kinds" decision.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarEffect), "Scalar")]
[JsonDerivedType(typeof(InvulnerableSaveEffect), "InvulnerableSave")]
[JsonDerivedType(typeof(WeaponCharacteristicEffect), "WeaponCharacteristic")]
[JsonDerivedType(typeof(FeelNoPainEffect), "FeelNoPain")]
[JsonDerivedType(typeof(WeaponKeywordGrantEffect), "WeaponKeywordGrant")]
[JsonDerivedType(typeof(NamedAbilityGrantEffect), "NamedAbilityGrant")]
[JsonDerivedType(typeof(NamedAbilityRemovalEffect), "NamedAbilityRemoval")]
[JsonDerivedType(typeof(RollModifierEffect), "RollModifier")]
public abstract record ClassificationEffect;

/// <summary>One atomic, unconditional mutation stated against exactly one named Statline scalar
/// characteristic ("M"/"T"/"Sv"/"W"/"Ld"/"Oc" - a plain string, not an enum, matching
/// <c>ScalarCharacteristicEffect.Characteristic</c>'s own convention). <see cref="Amount"/> is the
/// text's own stated value, unsigned/unresolved.</summary>
public sealed record ScalarEffect(string Characteristic, EffectVerb Verb, int Amount) : ClassificationEffect;

/// <summary>An unconditional invulnerable-save grant, as a melee/ranged pair - always Set-shaped.
/// An attack type the text doesn't name for this grant is 0 (no real 11e invulnerable save is ever
/// stated as "0+"/"1+").</summary>
public sealed record InvulnerableSaveEffect(int MeleeInSv, int RangedInSv) : ClassificationEffect;

/// <summary>One atomic, unconditional mutation stated against exactly one named WeaponProfile
/// characteristic ("S"/"A"/"AP"/"D"/"BS"/"WS"). A coordinate characteristic list in the source text
/// always splits into one record per named characteristic - never a single record holding more than
/// one. Weapons are picked as described on <see cref="AttackWeaponClass"/>.</summary>
public sealed record WeaponCharacteristicEffect(
    string Characteristic,
    EffectVerb Verb,
    int Amount,
    AttackWeaponClass? WeaponClass = null,
    string? WeaponName = null) : ClassificationEffect;

/// <summary>A Feel No Pain value grant - the corpus's single largest previously-unrepresented gap
/// (153 corpus hits at proposal time, zero representation anywhere in the codebase). Qualifier is
/// null for an unqualified grant, or a plain string from a known-but-growable vocabulary (e.g.
/// "Mortal Wounds", "Psychic Attacks") - not an enum, same lesson as
/// <see cref="WeaponKeywordGrantEffect.Keyword"/>.</summary>
public sealed record FeelNoPainEffect(int Value, string? Qualifier) : ClassificationEffect;

/// <summary>A named keyword grant (e.g. "[LETHAL HITS]") to a selected set of the bearer's weapons.
/// Keyword is a verbatim string, directly required by <c>retire-weapon-keyword-flags</c>'s own
/// lesson: a closed enum silently drops a renamed/new BSData keyword. <see cref="ReplacesKeyword"/> is
/// set when the text says the grant is "instead of" a keyword the weapon already has (e.g.
/// [SUSTAINED HITS 2] instead of [SUSTAINED HITS 1]).</summary>
public sealed record WeaponKeywordGrantEffect(
    string Keyword,
    string? ReplacesKeyword = null,
    AttackWeaponClass? WeaponClass = null,
    string? WeaponName = null) : ClassificationEffect;

/// <summary>A grant of a whole separate named ability (e.g. "this model has the Stealth ability").
/// AbilityName is verbatim, value included (e.g. Scouts 9"); <see cref="NameResolver"/> checks it
/// against BSData's ability names at collect.</summary>
public sealed record NamedAbilityGrantEffect(string AbilityName) : ClassificationEffect;

/// <summary>Removes a named ability (e.g. "lose the Dark Pacts ability"). Not resolved at collect: a
/// removal only acts on an ability the unit already displays, so an unmatched name is inert.</summary>
public sealed record NamedAbilityRemovalEffect(string AbilityName) : ClassificationEffect;

/// <summary>A modifier to one dice roll, recorded with the text's own verb; Damage/enemy/subtract also
/// covers reducing an attack's Damage characteristic. Only a Hit, Wound or Damage modifier picks
/// weapons, as described on <see cref="AttackWeaponClass"/>.</summary>
public sealed record RollModifierEffect(
    ModifiedRoll Roll,
    RollOwner RolledBy,
    RollModifierKind Modifier,
    int Amount,
    AttackWeaponClass? WeaponClass = null,
    string? WeaponName = null) : ClassificationEffect;
