using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

[JsonConverter(typeof(JsonStringEnumConverter<ModifiedRoll>))]
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

/// <summary>Whose roll a <see cref="RollModifierEffect"/> changes: the target's own, or an
/// opponent's roll against the target.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RollOwner>))]
public enum RollOwner
{
    [JsonStringEnumMemberName("friendly")] Friendly,
    [JsonStringEnumMemberName("enemy")] Enemy
}

[JsonConverter(typeof(JsonStringEnumConverter<RollModifierKind>))]
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
