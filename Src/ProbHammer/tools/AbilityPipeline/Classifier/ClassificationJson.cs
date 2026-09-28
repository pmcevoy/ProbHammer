using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>The one JSON contract every wire-format read/write of a
/// <see cref="Models.ClassificationResult"/> must share: camelCase property names, matching
/// exactly what <c>Anthropic.Helpers.StructuredOutput.CreateJsonFormat&lt;T&gt;()</c> generates
/// (confirmed via `schema` command output - "target"/"effects"/"residualConditionBucket"/etc, not
/// the POCOs' own PascalCase names). Used for both a few-shot assistant turn's own JSON
/// (<see cref="BatchRequestBuilder"/>) and parsing a real batch result back
/// (<c>Program.cs</c>'s collect command) - using two different option instances for these would
/// risk exactly the silent-drift bug this file exists to prevent.</summary>
public static class ClassificationJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // The structured-output grammar renders a nullable member as an optional key, never a literal
        // null, so few-shot turns must omit nulls to show the same shape the model is constrained to.
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // Models sometimes write an apostrophe as the literal text ' or \' inside a string, which
    // arrives in the raw JSON as \\u0027 / \\' - turn those back into a plain apostrophe.
    public static string CleanModelOutput(string json) =>
        json.Replace(@"\\u0027", "'").Replace(@"\\'", "'");
}