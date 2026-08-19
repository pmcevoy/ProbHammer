using System.Text.Json;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Deserializes a single BSData catalogueSchema JSON file's content into <see cref="BsCatalogue"/>.
/// Uses System.Text.Json only - no third-party JSON library, matching this project's existing
/// stance against third-party libraries for the equivalent XML case (CLAUDE.md).
/// </summary>
public static class BsdataCatalogueReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static BsCatalogue Read(string json)
    {
        var file = JsonSerializer.Deserialize<BsCatalogueFile>(json, Options);
        return file?.Catalogue ?? file?.GameSystem
            ?? throw new FormatException("BSData JSON file has no root 'catalogue' or 'gameSystem' object.");
    }
}
