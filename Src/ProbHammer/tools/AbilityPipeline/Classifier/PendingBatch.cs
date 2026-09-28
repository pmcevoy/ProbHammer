using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Tracks a submitted-but-not-yet-collected Batch API job across separate `submit`/
/// `collect` process runs (a batch can take up to 24h to complete, so a single blocking run isn't
/// practical) - written by `submit`, consumed and deleted by `collect` once the batch has fully
/// ended.</summary>
public sealed record PendingBatch
{
    public required string BatchId { get; init; }
    public required string PromptVersion { get; init; }
    public required string Model { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required int RequestCount { get; init; }
}

public static class PendingBatchFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static PendingBatch? Load(string path) =>
        File.Exists(path) ? JsonSerializer.Deserialize<PendingBatch>(File.ReadAllText(path), SerializerOptions) : null;

    public static void Save(string path, PendingBatch batch)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(batch, SerializerOptions));
    }

    public static void Delete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
