using System.Security.Cryptography;
using System.Text;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The content-hash key shared by the ability pipeline's corpus records and the runtime
/// <see cref="AbilityClassificationCatalogue"/> - both sides must hash identically or every lookup
/// silently misses.</summary>
public static class AbilityTextKey
{
    /// <summary>Folds known-harmless corpus authoring variance: a typographic U+2019 apostrophe and a
    /// U+00A0 non-breaking space in place of a plain one.</summary>
    public static string Normalize(string text) => text.Replace('’', '\'').Replace(' ', ' ');

    public static string Hash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(text))));
}