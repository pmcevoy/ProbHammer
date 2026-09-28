namespace ProbHammer.Tools.AbilityPipeline.Classifier.Models;

/// <summary>One effect plus its own condition triage - <c>RuleClassification</c>'s per-effect shape
/// for this pipeline, per design.md's Path B decision. <see cref="ConditionText"/> must be
/// populated whenever <see cref="ResidualConditionBucket"/> is not <c>None</c> - see the
/// "Human-readable condition text for any non-default condition" requirement; it is NOT reserved
/// for the Never bucket alone, so a uniform player-facing toggle always has a label to show.</summary>
public sealed record ClassifiedEffect
{
    public required ClassificationEffect Effect { get; init; }
    public required ResidualConditionBucket ResidualConditionBucket { get; init; }
    public string? ConditionText { get; init; }
    public ChoiceBranch? ChoiceBranch { get; init; }
}