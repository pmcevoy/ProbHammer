using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

public static class ExtractionFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyDictionary<string, ExtractionRecord> Load(string path)
    {
        var json = File.ReadAllText(path);
        var records = JsonSerializer.Deserialize<List<ExtractionRecord>>(json, SerializerOptions)
                       ?? throw new InvalidDataException($"'{path}' deserialized to no records.");
        return records.ToDictionary(r => r.Hash);
    }
}
