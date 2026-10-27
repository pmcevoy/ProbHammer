using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModifiedRoll
{
    [JsonStringEnumMemberName("Hit")] Hit,

    [JsonStringEnumMemberName("Wound")] Wound,

    [JsonStringEnumMemberName("Damage")] Damage,

    [JsonStringEnumMemberName("Save")] Save,

    [JsonStringEnumMemberName("Advance")] Advance,

    [JsonStringEnumMemberName("Charge")] Charge,

    [JsonStringEnumMemberName("BattleShock")] BattleShock
}

/// <summary>Whose roll it is: the target's own (<c>friendly</c>) or an opponent's roll against the
/// target (<c>enemy</c>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RollOwner
{
    [JsonStringEnumMemberName("friendly")] Friendly,

    [JsonStringEnumMemberName("enemy")] Enemy
}

/// <summary>Which weapons a weapon effect applies to, as flat fields rather than a selector union (a
/// union per effect kind made the output grammar too large): a <c>WeaponName</c> picks one weapon,
/// otherwise a class picks every weapon of it, and neither picks every weapon. Export turns these into
/// Core's <c>WeaponSelector</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackWeaponClass
{
    [JsonStringEnumMemberName("Melee")] Melee,

    [JsonStringEnumMemberName("Ranged")] Ranged
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RollModifierKind
{
    [JsonStringEnumMemberName("add")] Add,

    [JsonStringEnumMemberName("subtract")] Subtract,

    [JsonStringEnumMemberName("reroll")] Reroll,

    [JsonStringEnumMemberName("rerollOnes")] RerollOnes,

    [JsonStringEnumMemberName("rerollSingle")] RerollSingle,

    [JsonStringEnumMemberName("criticalOn")] CriticalOn,

    [JsonStringEnumMemberName("ignoreModifiers")] IgnoreModifiers
}
