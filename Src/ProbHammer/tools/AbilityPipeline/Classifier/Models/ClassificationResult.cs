namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>The full per-hash classification schema this pipeline's prompt v1 targets - the
/// structured-output shape a Batch API request's <c>OutputConfig</c> is generated from
/// (<c>Anthropic.Helpers.StructuredOutput.CreateJsonFormat&lt;ClassificationResult&gt;()</c>), so
/// <c>prompts/v1/schema.json</c> (generated from this same type) can never drift from what the
/// code actually requests. <see cref="UsageLimit"/>/<see cref="Phases"/>/<see cref="TurnOwnership"/>
/// are top-level, not per-effect - a usage limit or phase/turn restriction gates the ability's own
/// activation as a whole, per design.md. <see cref="Phases"/> lists every phase the ability can be
/// invoked in (e.g. "selected to shoot or fight"), empty when none is named.</summary>
public sealed record ClassificationResult
{
    public required ClassificationTarget Target { get; init; }
    public required IReadOnlyList<ClassifiedEffect> Effects { get; init; }
    public required IReadOnlyList<ChoiceGroup> ChoiceGroups { get; init; }
    public required IReadOnlyList<Phase> Phases { get; init; }
    public TurnOwnership? TurnOwnership { get; init; }
    public UsageLimit? UsageLimit { get; init; }
    public required CoverageStatus CoverageStatus { get; init; }
    public string? UnclassifiedResidue { get; init; }
}