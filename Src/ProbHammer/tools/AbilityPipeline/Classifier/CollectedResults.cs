namespace ProbHammer.Tools.AbilityPipeline.Classifier;

public static class CollectedResults
{
    /// <summary>Merges a run's records into the classification file, or with <paramref name="outPath"/>
    /// writes them alone to that file and leaves the classification file untouched.</summary>
    public static string Save(string classificationsPath, string? outPath,
        IReadOnlyDictionary<string, ClassificationRecord> collected)
    {
        if (outPath is not null)
        {
            ClassificationFile.Save(outPath, collected);
            return outPath;
        }

        var classifications = ClassificationFile.Load(classificationsPath).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        foreach (var (hash, record) in collected)
            classifications[hash] = record;
        ClassificationFile.Save(classificationsPath, classifications);
        return classificationsPath;
    }
}
