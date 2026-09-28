using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Builds one Batch API <see cref="Request"/> per pending hash - CustomID = hash (spec.md's
/// "Inspecting a classification's provenance" requirement relies on this 1:1 mapping to join a
/// result back to its corpus record), structured output validated against
/// <see cref="ClassificationResult"/>'s own schema via the SDK's native
/// <c>OutputConfig</c>/<c>StructuredOutput.CreateJsonFormat&lt;T&gt;()</c> mechanism - this
/// supersedes the forced-tool-use approach tasks.md originally envisioned; the SDK's current
/// surface achieves the identical "strict schema-validated JSON output" goal through its newer,
/// non-tool-based structured-output config instead.</summary>
public static class BatchRequestBuilder
{
    public const long MaxTokens = 8192;

    public static Request Build(ExtractionRecord record, string systemPrompt,
        IReadOnlyList<FewShotExample> fewShotExamples, IReadOnlyDictionary<string, ExtractionRecord> corpus,
        Model model)
    {
        var messages = new List<MessageParam>();

        for (var i = 0; i < fewShotExamples.Count; i++)
        {
            var example = fewShotExamples[i];
            if (!corpus.TryGetValue(example.Hash, out var exampleRecord))
                throw new InvalidOperationException(
                    $"Few-shot example {example.Hash} is not in the corpus, so its source kind is unknown.");

            messages.Add(new MessageParam
            {
                Role = Role.User,
                Content = FormatUserTurn(exampleRecord.SourceKind, example.Text)
            });

            var expectedJson = JsonSerializer.Serialize(example.ExpectedClassification, ClassificationJson.Options);
            // Cache breakpoint after the last example: system prompt + every example is the identical
            // prefix of every request. 1h TTL because a batch can take longer than the 5m default.
            messages.Add(new MessageParam
            {
                Role = Role.Assistant,
                Content = i == fewShotExamples.Count - 1
                    ? new List<ContentBlockParam>
                    {
                        new TextBlockParam
                        {
                            Text = expectedJson,
                            CacheControl = new CacheControlEphemeral { Ttl = Ttl.Ttl1h }
                        }
                    }
                    : expectedJson
            });
        }

        messages.Add(new MessageParam { Role = Role.User, Content = FormatUserTurn(record.SourceKind, record.Text) });

        return new Request
        {
            CustomID = record.Hash,
            Params = new Params
            {
                Model = model,
                MaxTokens = MaxTokens,
                System = systemPrompt,
                Messages = messages,
                OutputConfig = new OutputConfig
                {
                    Format = BuildFormat()
                }
            }
        };
    }

    // StructuredOutput's generated schema doesn't validate as-is against the real API for a
    // JsonPolymorphic union - confirmed via real Batch API test runs (2026-09-26):
    //  1. The node wrapping an `anyOf` also carries sibling `type`/`required` keys - rejected: "For
    //     'anyOf', 'required, type' is not supported". Strip them from that node.
    //  2. Each `anyOf` branch then has no `type` of its own - rejected: "Schema type is missing". Add
    //     `"type": "object"` back per branch.
    // Schema is IReadOnlyDictionary<string, JsonElement> (immutable), so every fix round-trips
    // through a mutable JsonNode tree rather than editing it in place.
    private static JsonOutputFormat BuildFormat()
    {
        var format = StructuredOutput.CreateJsonFormat<ClassificationResult>();
        var node = JsonSerializer.SerializeToNode(format.Schema)!.AsObject();
        FixSchemaNode(node);
        var fixedSchema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(node.ToJsonString())!;
        return format with { Schema = fixedSchema };
    }

    private static void FixSchemaNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("anyOf"))
                {
                    obj.Remove("type");
                    obj.Remove("required");
                }
                else if (obj.ContainsKey("properties") && !obj.ContainsKey("type"))
                {
                    obj["type"] = "object";
                }

                foreach (var property in obj.ToList())
                    FixSchemaNode(property.Value);
                break;
            case JsonArray array:
                foreach (var item in array)
                    FixSchemaNode(item);
                break;
        }
    }

    private static string FormatUserTurn(string sourceKind, string abilityText) =>
        $"Source kind: {sourceKind}\n\nClassify this ability/rule text:\n\n{abilityText}";
}