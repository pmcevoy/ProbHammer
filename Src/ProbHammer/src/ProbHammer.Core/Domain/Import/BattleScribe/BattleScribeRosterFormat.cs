using System.Text.Json;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;

namespace ProbHammer.Core.Domain.Import.BattleScribe;

/// <summary>
/// Recognizes and deserializes a BattleScribe/NewRecruit roster JSON export - see
/// battlescribe-roster-import's Format Recognition requirement. A payload is recognized only when
/// it parses as JSON and contains a top-level <c>roster</c> object whose <c>xmlns</c> identifies
/// the standard, cross-tool BattleScribe roster schema; anything else (plain GW-app export text,
/// or JSON of some other shape) is not treated as a BattleScribe roster export, letting it fall
/// through to the existing GW-app text pipeline unchanged.
/// </summary>
public static class BattleScribeRosterFormat
{
    public const string RosterSchemaXmlns = "http://www.battlescribe.net/schema/rosterSchema";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>True (with <paramref name="roster"/> populated) only for text that parses as JSON
    /// and carries the expected <c>roster.xmlns</c> value - false for non-JSON text, JSON without a
    /// <c>roster</c> object, or a <c>roster</c> object with a different/missing <c>xmlns</c>. Never
    /// throws - a malformed or unrelated payload is simply not recognized as this format.</summary>
    public static bool TryParse(string text, out BsRoster? roster)
    {
        roster = null;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("roster", out var rosterElement)
                || rosterElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!rosterElement.TryGetProperty("xmlns", out var xmlnsElement)
                || xmlnsElement.ValueKind != JsonValueKind.String
                || xmlnsElement.GetString() != RosterSchemaXmlns)
                return false;
        }

        var file = JsonSerializer.Deserialize<BsRosterFile>(text, Options);
        roster = file?.Roster;
        return roster is not null;
    }
}
