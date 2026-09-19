namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Tokenizes a weapon profile's free-text "Keywords" characteristic (e.g.
/// "Anti-infantry 4+, Devastating Wounds") into its verbatim token list - no per-token
/// recognition, no flag mapping. <see cref="KeywordsText"/> is the sole representation of a
/// weapon's ability keywords.
/// </summary>
public static class WeaponKeywordParser
{
    /// <summary>
    /// Splits <paramref name="keywordsText"/> into its verbatim token list and stamps it onto
    /// <paramref name="weapon"/>. A value of "-" (BSData's "no keywords" marker) or blank
    /// produces an empty list, not a parse failure.
    /// </summary>
    public static WeaponProfile Apply(WeaponProfile weapon, string keywordsText)
    {
        return weapon with { KeywordsText = Tokenize(keywordsText) };
    }

    private static IReadOnlyList<string> Tokenize(string keywordsText)
    {
        if (string.IsNullOrWhiteSpace(keywordsText) || keywordsText.Trim() == "-")
            return [];

        return keywordsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}