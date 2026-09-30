using System.Runtime.CompilerServices;
using System.Text.Json;
using Anthropic;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;
using ProbHammer.Tools.AbilityPipeline.Classifier;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string promptVersion = "v2";
const Model model = Model.ClaudeSonnet5;

var dataDir = Path.Combine(RepoRoot(), "tools", "AbilityPipeline", "data");
var corpusPath = Path.Combine(dataDir, "ability-corpus.json");
var classificationsPath = Path.Combine(dataDir, "classifications.json");
var pendingBatchPath = Path.Combine(dataDir, "pending-batch.json");
var vocabularyPath = Path.Combine(dataDir, "vocabulary.json");
var promptDir = Path.Combine(RepoRoot(), "tools", "AbilityPipeline", "prompts", promptVersion);
var fewShotPath = Path.Combine(RepoRoot(), "tools", "AbilityPipeline", "fewshot", "examples.json");

if (args.Length == 0)
{
    Console.WriteLine(
        "Usage: Classifier <submit [N|hash1,hash2,...]|collect|resolve|status|schema|check-fewshot|preview <hash>|report [file]>");
    return 1;
}

return args[0] switch
{
    "submit" => await SubmitAsync(),
    "collect" => await CollectAsync(),
    "resolve" => ResolveAll(),
    "status" => await StatusAsync(),
    "schema" => WriteSchema(),
    "check-fewshot" => CheckFewShot(),
    "preview" => Preview(),
    "report" => Report(args.Length > 1 ? Path.GetFullPath(args[1]) : classificationsPath),
    _ => Unknown()
};

int Unknown()
{
    Console.WriteLine(
        $"Unknown command '{args[0]}'. Use submit, collect, resolve, status, schema, check-fewshot, preview, or report.");
    return 1;
}

// Splits a classifications file into one file per coverage status, for review - the source file stays
// the single store the incremental selector reads.
int Report(string sourcePath)
{
    var records = ClassificationFile.Load(sourcePath);
    var outDir = Path.Combine(Path.GetDirectoryName(sourcePath)!, "by-coverage");
    var baseName = Path.GetFileNameWithoutExtension(sourcePath);

    Console.WriteLine($"{records.Count} classifications in '{sourcePath}':");
    foreach (var status in Enum.GetValues<CoverageStatus>())
    {
        var subset = records
            .Where(kvp => kvp.Value.EffectiveCoverageStatus == status)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var path = Path.Combine(outDir, $"{baseName}.{status.ToString().ToLowerInvariant()}.json");
        ClassificationFile.Save(path, subset);
        Console.WriteLine($"  {status,-15} {subset.Count,5}  -> {path}");
    }

    return 0;
}

// Re-applies name resolution to every existing record - after a vocabulary refresh, or to records
// collected before resolution existed. Classification itself is untouched, so this is idempotent.
int ResolveAll()
{
    if (LoadResolver() is not { } resolver)
        return 1;

    var records = ClassificationFile.Load(classificationsPath)
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { Resolution = resolver.Resolve(kvp.Value.Classification) });
    ClassificationFile.Save(classificationsPath, records);
    return Report(classificationsPath);
}

NameResolver? LoadResolver()
{
    if (File.Exists(vocabularyPath))
        return new NameResolver(CorpusVocabulary.Load(vocabularyPath));

    Console.WriteLine($"No vocabulary file at '{vocabularyPath}' - run the Extractor first.");
    return null;
}

int Preview()
{
    var corpus = ExtractionFile.Load(corpusPath);
    if (args.Length < 2 || !corpus.TryGetValue(args[1], out var record))
    {
        Console.WriteLine("Usage: Classifier preview <hash> (a hash present in ability-corpus.json)");
        return 1;
    }

    var request = BatchRequestBuilder.Build(record, File.ReadAllText(Path.Combine(promptDir, "system-prompt.md")),
        FewShotFile.Load(fewShotPath), corpus, model);
    Console.WriteLine(request.ToString());
    return 0;
}

int CheckFewShot()
{
    var examples = FewShotFile.Load(fewShotPath);
    var corpus = ExtractionFile.Load(corpusPath);
    Console.WriteLine($"Loaded {examples.Count} few-shot examples from '{fewShotPath}':");
    var missing = 0;
    foreach (var example in examples)
    {
        var sourceKind = corpus.TryGetValue(example.Hash, out var record) ? record.SourceKind : "NOT IN CORPUS";
        if (record is null)
            missing++;
        Console.WriteLine($"  {example.Hash[..12]}...  {example.ExpectedClassification.Target.GetType().Name,-20} " +
                          $"{example.ExpectedClassification.Effects.Count} effect(s)  {sourceKind}");
    }

    return missing == 0 ? 0 : 1;
}

int WriteSchema()
{
    // Generated FROM ClassificationResult, not hand-authored, so prompts/v{N}/schema.json can never
    // drift from the POCOs the classifier actually submits (task 2.2/3.2's "matching" requirement) -
    // see design.md's "Extraction's own output schema is deliberately simple and stable" note, applied
    // here to the classification schema instead.
    var schema = StructuredOutput.ToJsonSchema<ClassificationResult>();
    var schemaPath = Path.Combine(promptDir, "schema.json");
    Directory.CreateDirectory(promptDir);
    File.WriteAllText(schemaPath, schema.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }));
    Console.WriteLine($"Wrote schema to '{schemaPath}'.");
    return 0;
}

async Task<int> SubmitAsync()
{
    if (!File.Exists(corpusPath))
    {
        Console.WriteLine($"No corpus file at '{corpusPath}' - run the Extractor first.");
        return 1;
    }

    if (PendingBatchFile.Load(pendingBatchPath) is { } existingPending)
    {
        Console.WriteLine(
            $"A batch is already pending ({existingPending.BatchId}, submitted {existingPending.SubmittedAt}) - " +
            "run 'collect' first, or delete pending-batch.json if it's stale.");
        return 1;
    }

    var corpus = ExtractionFile.Load(corpusPath);
    var existingClassifications = ClassificationFile.Load(classificationsPath);
    var pending = IncrementalSelector.SelectPending(corpus, existingClassifications, promptVersion);

    // Optional args[1]: an integer takes the first N pending (existing alphabetical order); a
    // comma-separated list of hashes selects exactly those, regardless of order - lets a smoke test
    // target a specific known-interesting record without waiting on alphabetical luck.
    if (args.Length > 1)
    {
        pending = int.TryParse(args[1], out var limit)
            ? pending.Take(limit).ToList()
            : pending.Where(record => args[1].Split(',').Contains(record.Hash)).ToList();
    }

    if (pending.Count == 0)
    {
        Console.WriteLine("Nothing to classify - every corpus hash already has a current classification.");
        return 0;
    }

    var systemPrompt = File.ReadAllText(Path.Combine(promptDir, "system-prompt.md"));
    var fewShotExamples = FewShotFile.Load(fewShotPath);

    var requests = pending
        .Select(record => BatchRequestBuilder.Build(record, systemPrompt, fewShotExamples, corpus, model))
        .ToList();

    Console.WriteLine($"Submitting {requests.Count} requests (prompt {promptVersion}, model {model}) ...");

    using var client = NewClient();
    var batch = await client.Messages.Batches.Create(new BatchCreateParams { Requests = requests });

    PendingBatchFile.Save(pendingBatchPath, new PendingBatch
    {
        BatchId = batch.ID,
        PromptVersion = promptVersion,
        Model = model.ToString(),
        SubmittedAt = DateTimeOffset.UtcNow,
        RequestCount = requests.Count
    });

    Console.WriteLine(
        $"Submitted batch '{batch.ID}' - status: {batch.ProcessingStatus}. Run 'collect' later to retrieve results.");
    return 0;
}

async Task<int> StatusAsync()
{
    var pending = PendingBatchFile.Load(pendingBatchPath);
    if (pending is null)
    {
        Console.WriteLine("No pending batch.");
        return 0;
    }

    using var client = NewClient();
    var batch = await client.Messages.Batches.Retrieve(pending.BatchId, new BatchRetrieveParams());
    Console.WriteLine($"Batch '{batch.ID}': {batch.ProcessingStatus} " +
                      $"(succeeded={batch.RequestCounts.Succeeded}, errored={batch.RequestCounts.Errored}, " +
                      $"processing={batch.RequestCounts.Processing}, canceled={batch.RequestCounts.Canceled}, " +
                      $"expired={batch.RequestCounts.Expired})");
    return 0;
}

async Task<int> CollectAsync()
{
    var pending = PendingBatchFile.Load(pendingBatchPath);
    if (pending is null)
    {
        Console.WriteLine("No pending batch to collect - run 'submit' first.");
        return 1;
    }

    if (LoadResolver() is not { } resolver)
        return 1;

    using var client = NewClient();
    var batch = await client.Messages.Batches.Retrieve(pending.BatchId, new BatchRetrieveParams());

    ProcessingStatus processingStatus = batch.ProcessingStatus;
    if (processingStatus != ProcessingStatus.Ended)
    {
        Console.WriteLine($"Batch '{batch.ID}' is still {batch.ProcessingStatus} - not ready yet.");
        return 0;
    }

    var corpus = ExtractionFile.Load(corpusPath);
    var classifications = ClassificationFile.Load(classificationsPath).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    var succeeded = 0;
    var failed = 0;
    long inputTokens = 0, outputTokens = 0, cacheReadTokens = 0, cacheWriteTokens = 0;
    var usageCount = 0;

    await foreach (var response in client.Messages.Batches.ResultsStreaming(pending.BatchId, new BatchResultsParams()))
    {
        if (!response.Result.TryPickSucceeded(out var success))
        {
            failed++;
            var detail = response.Result.TryPickErrored(out var errored)
                ? $"{errored.Error.Error.Type}: {errored.Error.Error.Message}"
                : response.Result.Type.ToString();
            Console.WriteLine($"  {response.CustomID}: not succeeded ({detail})");
            continue;
        }

        var usage = success.Message.Usage;
        inputTokens += usage.InputTokens;
        outputTokens += usage.OutputTokens;
        cacheReadTokens += usage.CacheReadInputTokens ?? 0;
        cacheWriteTokens += usage.CacheCreationInputTokens ?? 0;
        usageCount++;

        var textBlock = success.Message.Content
            .Select(block => block.TryPickText(out var text) ? text : null)
            .FirstOrDefault(text => text is not null);

        if (textBlock is null)
        {
            failed++;
            Console.WriteLine($"  {response.CustomID}: succeeded response carried no text block");
            continue;
        }

        if (!corpus.TryGetValue(response.CustomID, out var corpusRecord))
        {
            failed++;
            Console.WriteLine($"  {response.CustomID}: no matching corpus record (hash removed since submission?)");
            continue;
        }

        // StructuredOutput.Parse<T>() constrains T : new(), which ClassificationResult (required
        // members, per design.md Principle #1's discriminated-union convention) can't satisfy - plain
        // System.Text.Json deserialization handles `required` natively with no such constraint.
        ClassificationResult classification;
        try
        {
            classification =
                JsonSerializer.Deserialize<ClassificationResult>(
                    ClassificationJson.CleanModelOutput(textBlock.Text), ClassificationJson.Options)
                ?? throw new InvalidDataException($"Empty classification JSON for {response.CustomID}");
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            failed++;
            Console.WriteLine($"  {response.CustomID}: unparseable classification JSON ({ex.Message})");
            continue;
        }

        classifications[response.CustomID] = new ClassificationRecord
        {
            Hash = response.CustomID,
            Text = corpusRecord.Text,
            PromptVersion = pending.PromptVersion,
            Model = pending.Model,
            ClassifiedAt = DateTimeOffset.UtcNow,
            Classification = classification,
            Resolution = resolver.Resolve(classification),
            ReviewStatus = ReviewStatus.Pending
        };
        succeeded++;
    }

    ClassificationFile.Save(classificationsPath, classifications);
    PendingBatchFile.Delete(pendingBatchPath);

    Console.WriteLine($"Collected {succeeded} classifications ({failed} failed/skipped) into '{classificationsPath}'.");
    if (usageCount > 0)
        Console.WriteLine($"Usage ({pending.Model}, {usageCount} responses): input={inputTokens:N0} " +
                          $"output={outputTokens:N0} cacheRead={cacheReadTokens:N0} cacheWrite={cacheWriteTokens:N0} | " +
                          $"per response: input={inputTokens / usageCount:N0} output={outputTokens / usageCount:N0}");
    return Report(classificationsPath);
}

static string RepoRoot([CallerFilePath] string here = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

// The zero-arg AnthropicClient() constructor only auto-resolves ANTHROPIC_API_KEY (or an OAuth
// profile) - this project deliberately keys its own credential under a project-specific name
// instead, so it needs to be read and passed explicitly.
static AnthropicClient NewClient() => new()
{
    ApiKey = Environment.GetEnvironmentVariable("PROBHAMMER_API_KEY")
             ?? throw new InvalidOperationException("PROBHAMMER_API_KEY environment variable is not set.")
};