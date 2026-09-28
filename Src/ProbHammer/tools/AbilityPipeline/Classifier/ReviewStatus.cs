using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>A classification record's human-review state - see spec.md's "Mandatory human review
/// before trust" requirement: a freshly produced classification always starts
/// <see cref="Pending"/>, never auto-approved.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}
