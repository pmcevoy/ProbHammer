using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Standalone mirror of <c>ProbHammer.Core.Domain.Catalogue.WeaponSelector</c> - which
/// weapons owned by a <see cref="WeaponCharacteristicEffect"/>/<see cref="WeaponKeywordGrantEffect"/>'s
/// bearer a mutation/grant applies to.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AllWeaponsSelector), "AllWeapons")]
[JsonDerivedType(typeof(WeaponClassSelector), "WeaponClass")]
[JsonDerivedType(typeof(NamedWeaponSelector), "NamedWeapon")]
public abstract record WeaponSelector;

/// <summary>Every weapon the bearer is equipped with, unqualified by type or name.</summary>
public sealed record AllWeaponsSelector : WeaponSelector;

/// <summary>Every weapon of one type ("Melee" or "Ranged") the bearer is equipped with. Plain
/// string, not a shared enum with Core, per this project's "no cross-project shared vocabulary"
/// decoupling decision.</summary>
public sealed record WeaponClassSelector(string Type) : WeaponSelector;

/// <summary>One specifically named weapon the bearer is equipped with (e.g. "this model's bolt
/// rifle").</summary>
public sealed record NamedWeaponSelector(string Name) : WeaponSelector;
