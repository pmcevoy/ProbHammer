using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>Whole-record coverage triage - see design.md's "Coverage status per classification, not
/// a numeric confidence score" decision. Recovers the role the retired regex classifier's
/// caveat flag played (Principle #6: "Never silently claim full
/// understanding of partially-understood text"), generalized to a category plus a plain-English
/// note rather than a single boolean or a poorly-calibrated numeric score. Distinct from per-effect
/// <see cref="ResidualConditionBucket"/>: that triages a condition on an effect that WAS extracted;
/// this flags a whole clause of the text that wasn't extracted as any Effect at all (e.g. an OR'd
/// choice between effects, or an effect kind - mortal-wound infliction, movement/terrain interaction
/// - this schema has no representation for yet).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoverageStatus
{
    /// <summary>Every clause of the text is represented in Target/Effects. No residue.</summary>
    [JsonStringEnumMemberName("complete")]
    Complete,

    /// <summary>At least one effect was extracted, but some other stated clause was not -
    /// <see cref="ClassificationResult.UnclassifiedResidue"/> says what.</summary>
    [JsonStringEnumMemberName("partial")]
    Partial,

    /// <summary>The text clearly states something real, but none of it maps onto this schema -
    /// <c>Effects</c> is empty and <see cref="ClassificationResult.UnclassifiedResidue"/> says what
    /// was missed. Distinct from a genuine negative control (no stated effect at all), which is
    /// <see cref="Complete"/> with empty Effects and null residue.</summary>
    [JsonStringEnumMemberName("unclassifiable")]
    Unclassifiable
}
