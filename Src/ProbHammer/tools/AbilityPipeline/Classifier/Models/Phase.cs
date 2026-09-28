using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Standalone mirror of <c>ProbHammer.Core.Domain.Roster.GamePhase</c> - the five phases
/// of a battle round, in play order.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Phase
{
    [JsonStringEnumMemberName("command")] Command,

    [JsonStringEnumMemberName("movement")] Movement,

    [JsonStringEnumMemberName("shooting")] Shooting,

    [JsonStringEnumMemberName("charge")] Charge,

    [JsonStringEnumMemberName("fight")] Fight
}

/// <summary>Standalone mirror of <c>ProbHammer.Core.Domain.Roster.GameTurn</c> - which player's
/// turn a restriction applies to. Null when the text states no turn-ownership restriction.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TurnOwnership
{
    [JsonStringEnumMemberName("mine")] Mine,

    [JsonStringEnumMemberName("theirs")] Theirs
}