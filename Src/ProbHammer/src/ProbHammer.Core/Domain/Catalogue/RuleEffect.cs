using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>One thing a rule/ability's text states it does. Magnitudes stay as the text states them,
/// unsigned and unresolved - see <see cref="EffectVerb"/>. Whether it applies on its own is decided
/// by its <see cref="ClassifiedEffect"/> wrapper, not here.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ScalarCharacteristicEffect), "Scalar")]
[JsonDerivedType(typeof(InvulnerableSaveCharacteristicEffect), "InvulnerableSave")]
[JsonDerivedType(typeof(WeaponCharacteristicEffect), "WeaponCharacteristic")]
[JsonDerivedType(typeof(FeelNoPainEffect), "FeelNoPain")]
[JsonDerivedType(typeof(WeaponKeywordGrantEffect), "WeaponKeywordGrant")]
[JsonDerivedType(typeof(NamedAbilityGrantEffect), "NamedAbilityGrant")]
[JsonDerivedType(typeof(NamedAbilityRemovalEffect), "NamedAbilityRemoval")]
public abstract record RuleEffect;

/// <summary>A change to one <see cref="Statline"/> scalar, named by its property ("M"/"T"/"Sv"/"W"/
/// "Ld"/"Oc").</summary>
public sealed record ScalarCharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount)
    : RuleEffect;

/// <summary>An invulnerable-save grant as a melee/ranged pair; a side the text doesn't name is
/// <c>0</c>, <see cref="InvulnerableSave.None"/>'s own sentinel.</summary>
public sealed record InvulnerableSaveCharacteristicEffect(InvulnerableSave Value) : RuleEffect;

/// <summary>A change to one <see cref="WeaponProfile"/> characteristic ("S"/"A"/"AP"/"D"/"BS"/"WS") of the
/// weapons <see cref="Selector"/> picks out.</summary>
public sealed record WeaponCharacteristicEffect(
    WeaponSelector Selector,
    string Characteristic,
    EffectVerb Verb,
    int Amount) : RuleEffect;

/// <summary>Data only - no consumer yet.</summary>
public sealed record FeelNoPainEffect(int Value, string? Qualifier = null) : RuleEffect;

/// <summary>Grants <see cref="Keyword"/> to the weapons <see cref="Selector"/> picks out; resolved by
/// <see cref="WeaponKeywordGrantResolver"/>. <see cref="ReplacesKeyword"/> is not consulted - keyword
/// identity already finds what a better value replaces.</summary>
public sealed record WeaponKeywordGrantEffect(WeaponSelector Selector, string Keyword, string? ReplacesKeyword = null)
    : RuleEffect;

/// <summary>Data only - no consumer yet.</summary>
public sealed record NamedAbilityGrantEffect(string AbilityName) : RuleEffect;

/// <summary>Data only - no consumer yet.</summary>
public sealed record NamedAbilityRemovalEffect(string AbilityName) : RuleEffect;