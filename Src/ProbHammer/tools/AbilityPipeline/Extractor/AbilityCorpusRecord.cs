namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>One distinct, normalized-text record in <c>ability-corpus.json</c>. Keyed by
/// <see cref="Hash"/> (a stable hash over the normalized text alone - see
/// <see cref="ProbHammer.Core.Domain.Catalogue.AbilityTextKey"/>), never by <see cref="Names"/>, since BSData reuses identical rule
/// text under many different wargear/ability names.</summary>
public sealed record AbilityCorpusRecord
{
    public required string Hash { get; init; }
    public required string Text { get; init; }
    public required IReadOnlyList<string> Names { get; init; }
    public required string SourceKind { get; init; }
    public required DateOnly FirstSeenAt { get; init; }
    public required DateOnly LastSeenAt { get; init; }
}
