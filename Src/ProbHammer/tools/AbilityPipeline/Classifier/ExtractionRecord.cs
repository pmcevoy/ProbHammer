namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Read-only, standalone mirror of the Extractor's own <c>AbilityCorpusRecord</c> shape -
/// duplicated rather than shared, per this project's "Classifier does NOT reference
/// ProbHammer.Core" decoupling (and by extension, not the Extractor project either, since it
/// references Core). Field names match <c>ability-corpus.json</c>'s own PascalCase property names
/// exactly so it deserializes with no naming policy required.</summary>
public sealed record ExtractionRecord
{
    public required string Hash { get; init; }
    public required string Text { get; init; }
    public required IReadOnlyList<string> Names { get; init; }
    public required string SourceKind { get; init; }
    public required DateOnly FirstSeenAt { get; init; }
    public required DateOnly LastSeenAt { get; init; }
}
