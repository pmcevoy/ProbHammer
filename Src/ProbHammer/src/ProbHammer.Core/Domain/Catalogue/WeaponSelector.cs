using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed <see cref="AllWeapons"/>/<see cref="WeaponClass"/>/
/// <see cref="NamedWeapon"/> subtypes, never null - which weapons owned by a
/// <see cref="WeaponCharacteristicEffect"/>'s bearer a mutation applies to is always exactly one of
/// these, mirroring <see cref="RuleTarget"/>'s own abstract-base/sealed-subtype convention. Every
/// real Phase 1 corpus example resolves to <see cref="WeaponClass"/>; <see cref="AllWeapons"/>/
/// <see cref="NamedWeapon"/> are kept for completeness (the phrasing space is a predictable, narrow
/// extension of the same pattern family), not required to be exercised by a real example - same
/// precedent <see cref="UnconditionalRuleTarget"/> already sets. The
/// <see cref="JsonPolymorphicAttribute"/>/<see cref="JsonDerivedTypeAttribute"/> pair mirrors
/// <see cref="RuleTarget"/>'s own convention exactly.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AllWeapons), "AllWeapons")]
[JsonDerivedType(typeof(WeaponClass), "WeaponClass")]
[JsonDerivedType(typeof(NamedWeapon), "NamedWeapon")]
public abstract record WeaponSelector;

/// <summary>Every weapon the bearer is equipped with, unqualified by type or name (e.g. "weapons
/// equipped by this model" with no "melee"/"ranged" qualifier).</summary>
public sealed record AllWeapons : WeaponSelector;

/// <summary>Every weapon of one <see cref="WeaponType"/> the bearer is equipped with (e.g. "melee
/// weapons equipped by this model"). Reuses the existing <see cref="WeaponType"/> enum rather than
/// inventing a parallel Melee/Ranged distinction.</summary>
public sealed record WeaponClass(WeaponType Type) : WeaponSelector;

/// <summary>One specifically named weapon the bearer is equipped with (e.g. "this model's bolt
/// rifle"). No real Phase 1 corpus example exercises this shape yet.</summary>
public sealed record NamedWeapon(string Name) : WeaponSelector;
