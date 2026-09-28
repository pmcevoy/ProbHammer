using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Per-effect condition triage - see design.md's "Condition/Phase/TurnOwnership -
/// CONFIRMED, Path B" decision. <see cref="EvaluableNow"/> is a marked candidate for a later engine
/// to compute automatically from roster state; <see cref="Never"/> and
/// <see cref="ClassificationResult.UsageLimit"/>/phase-restricted conditions will always rely on
/// player assertion instead.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResidualConditionBucket
{
    /// <summary>No condition beyond the effect itself applying unconditionally to its Target.</summary>
    [JsonStringEnumMemberName("none")]
    None,

    /// <summary>Gated by a condition determinable from the roster's own tracked state (e.g. current
    /// wounds versus starting wounds).</summary>
    [JsonStringEnumMemberName("evaluable-now")]
    EvaluableNow,

    /// <summary>Gated by a condition with no domain representation (e.g. board position).</summary>
    [JsonStringEnumMemberName("never")]
    Never
}
