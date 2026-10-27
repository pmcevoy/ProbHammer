using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Tests;

internal static class Fixtures
{
    public static readonly DateOnly Today = new(2026, 10, 26);

    public static ClassificationResult Classification(params ClassificationEffect[] effects) => new()
    {
        Target = new SelfTarget(),
        Effects =
        [
            .. effects.Select(e => new ClassifiedEffect
                { Effect = e, ResidualConditionBucket = ResidualConditionBucket.None })
        ],
        ChoiceGroups = [],
        Phases = [],
        CoverageStatus = CoverageStatus.Complete
    };

    public static ClassificationRecord Record(string hash, ClassificationResult classification) => new()
    {
        Hash = hash,
        Text = $"Text of {hash}.",
        PromptVersion = "v3",
        Model = "ClaudeSonnet5",
        ClassifiedAt = DateTimeOffset.UnixEpoch,
        Classification = classification
    };

    public static ExtractionRecord Extraction(string hash, string text, DateOnly? lastSeenAt = null) => new()
    {
        Hash = hash,
        Text = text,
        Names = [$"Name of {hash}"],
        SourceKind = "Datasheet ability",
        FirstSeenAt = Today,
        LastSeenAt = lastSeenAt ?? Today
    };

    public static CorpusVocabulary Vocabulary(IEnumerable<string>? weaponNames = null) => new(
        new Dictionary<string, int>(),
        (weaponNames ?? []).ToDictionary(n => n, _ => 1),
        new Dictionary<string, int>(),
        new Dictionary<string, int>());
}