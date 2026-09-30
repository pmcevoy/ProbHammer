using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Closed, small vocabulary - a rulebook-level mechanical concept, not BSData content
/// that grows with new codexes, per design.md's "UsageLimit - a fourth top-level field" decision.
/// Null means the ability carries no usage-frequency restriction at all.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UsageLimit
{
    [JsonStringEnumMemberName("Once per battle")]
    OncePerBattle,

    [JsonStringEnumMemberName("Twice per battle")]
    TwicePerBattle,

    [JsonStringEnumMemberName("Once per battle round")]
    OncePerBattleRound,

    [JsonStringEnumMemberName("Once per turn")]
    OncePerTurn,

    [JsonStringEnumMemberName("Once per phase")]
    OncePerPhase
}
