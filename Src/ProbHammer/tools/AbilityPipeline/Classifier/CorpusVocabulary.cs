using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Reads the extractor's <c>vocabulary.json</c>: every BSData spelling of each name kind,
/// with its occurrence count.</summary>
public sealed record CorpusVocabulary(
    IReadOnlyDictionary<string, int> WeaponKeywords,
    IReadOnlyDictionary<string, int> WeaponNames,
    IReadOnlyDictionary<string, int> UnitKeywords,
    IReadOnlyDictionary<string, int> AbilityNames)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static CorpusVocabulary Load(string path) =>
        JsonSerializer.Deserialize<CorpusVocabulary>(File.ReadAllText(path), SerializerOptions)
        ?? throw new InvalidDataException($"Empty vocabulary file '{path}'.");
}