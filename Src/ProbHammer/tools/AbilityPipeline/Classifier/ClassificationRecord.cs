using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>One row of <c>classifications.json</c> - a hash's classification plus full provenance
/// (prompt version, model, timestamp) and human-review state. See spec.md's "Versioned prompts and
/// schema" and "Mandatory human review before trust" requirements.</summary>
public sealed record ClassificationRecord
{
    public required string Hash { get; init; }

    /// <summary>The exact text classified, carried alongside the hash so a person inspecting this
    /// record doesn't need to cross-reference ability-corpus.json - and so a change in the source
    /// text (a different value here than the corpus's current text for this hash) is directly
    /// visible without recomputing anything.</summary>
    public required string Text { get; init; }

    public required string PromptVersion { get; init; }
    public required string Model { get; init; }
    public required DateTimeOffset ClassifiedAt { get; init; }
    public required ClassificationResult Classification { get; init; }
    public NameResolution? Resolution { get; init; }

    /// <summary>The model's coverage, demoted from <c>complete</c> to <c>partial</c> when a name failed
    /// to resolve - kept separate so <see cref="Classification"/> stays exactly what the model said.</summary>
    public CoverageStatus EffectiveCoverageStatus =>
        Resolution is { Unresolved.Count: > 0 } && Classification.CoverageStatus == CoverageStatus.Complete
            ? CoverageStatus.Partial
            : Classification.CoverageStatus;

    public ReviewStatus ReviewStatus { get; init; } = ReviewStatus.Pending;
    public string? ReviewerNote { get; init; }
}