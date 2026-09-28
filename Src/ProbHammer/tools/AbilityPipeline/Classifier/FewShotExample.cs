using System.Text.Json;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>One curated {hash, text, expectedClassification} triple from
/// <c>fewshot/examples.json</c> - shared across every prompt version (see design.md: "Few-shot
/// examples live in ONE shared file, not duplicated per prompt version").</summary>
public sealed record FewShotExample
{
    public required string Hash { get; init; }
    public required string Text { get; init; }
    public required ClassificationResult ExpectedClassification { get; init; }
}

public static class FewShotFile
{
    public static IReadOnlyList<FewShotExample> Load(string path)
    {
        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<FewShotExample>>(json, ClassificationJson.Options) ?? [];
    }
}