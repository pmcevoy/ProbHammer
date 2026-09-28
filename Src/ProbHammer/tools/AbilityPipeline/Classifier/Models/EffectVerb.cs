using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Standalone mirror of <c>ProbHammer.Core.Domain.Catalogue.EffectVerb</c> - the rulebook
/// vocabulary an effect states, not pre-resolved signed arithmetic. Sign/clamp resolution against a
/// characteristic's own arithmetic family is a separate, later concern - see design.md Principle
/// #3.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectVerb
{
    [JsonStringEnumMemberName("improve")] Improve,

    [JsonStringEnumMemberName("worsen")] Worsen,

    [JsonStringEnumMemberName("set")] Set
}