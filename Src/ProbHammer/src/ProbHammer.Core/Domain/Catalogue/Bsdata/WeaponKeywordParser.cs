using System.Text.RegularExpressions;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Parses a weapon profile's free-text "Keywords" characteristic (e.g.
/// "Anti-infantry 4+, Devastating Wounds") into a set of flag mutations to apply to a
/// <see cref="WeaponProfile"/>, plus the verbatim token list every token unconditionally joins
/// (see datasheet-catalogue's "Weapon Profile Verbatim Keyword Text" requirement). Only tokens
/// whose mapping to an existing flag is exact and unambiguous are recognized - no alias/synonym
/// recognition (see design.md's deferred-alias decision: "Cleave" is never mapped to Blast, nor
/// "Close Combat" to Pistol).
/// </summary>
public static partial class WeaponKeywordParser
{
    [GeneratedRegex(@"^Anti-(.+)\s+(\d+)\+$")]
    private static partial Regex AntiPattern();

    [GeneratedRegex(@"^Melta\s+(\d+)$")]
    private static partial Regex MeltaPattern();

    [GeneratedRegex(@"^Rapid Fire\s+(\d+)$")]
    private static partial Regex RapidFirePattern();

    [GeneratedRegex(@"^Sustained Hits\s+(\d+)$")]
    private static partial Regex SustainedHitsPattern();

    /// <summary>
    /// Applies every recognized token in <paramref name="keywordsText"/> to <paramref name="weapon"/>
    /// and stamps its verbatim token list. A value of "-" (BSData's "no keywords" marker) produces
    /// no flags and an empty verbatim list, not a parse failure.
    /// </summary>
    public static WeaponProfile Apply(WeaponProfile weapon, string keywordsText)
    {
        if (string.IsNullOrWhiteSpace(keywordsText) || keywordsText.Trim() == "-")
            return weapon with { KeywordsText = [] };

        var tokens = keywordsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        weapon = weapon with { KeywordsText = tokens };

        foreach (var token in tokens)
        {
            if (TryRecognize(token, weapon, out var updated))
                weapon = updated;
        }

        return weapon;
    }

    /// <summary>
    /// Every token in <paramref name="keywordsText"/> that does not exactly match an existing
    /// <see cref="WeaponProfile"/> flag - the same recognition rules <see cref="Apply"/> uses,
    /// shared through <see cref="TryRecognize"/> rather than duplicated, so this can never drift
    /// from what <see cref="Apply"/> actually recognizes (see the full-BSData-corpus scan's
    /// design.md). A value of "-" (or blank) yields an empty list, matching <see cref="Apply"/>'s
    /// own handling of "no keywords".
    /// </summary>
    public static IReadOnlyList<string> UnrecognizedTokens(string keywordsText)
    {
        if (string.IsNullOrWhiteSpace(keywordsText) || keywordsText.Trim() == "-")
            return [];

        var tokens = keywordsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return tokens.Where(token => !TryRecognize(token, ProbeWeapon, out _)).ToList();
    }

    /// <summary>Throwaway concrete instance <see cref="UnrecognizedTokens"/> threads through
    /// <see cref="TryRecognize"/> purely to satisfy its signature - <c>WeaponProfile</c> is
    /// abstract, and the produced <c>updated</c> value is discarded, so only recognition (the
    /// bool return) matters here.</summary>
    private static readonly WeaponProfile ProbeWeapon = new RangedWeapon("", 0, 0, 0, 0, 0, 0);

    /// <summary>Recognizes one token against the exact, unambiguous vocabulary this parser
    /// models (see the class doc comment) and, if recognized, returns the flag mutation applied
    /// to <paramref name="weapon"/> via <paramref name="updated"/>. Shared by <see cref="Apply"/>
    /// (which threads <paramref name="weapon"/> through every token in a keyword list) and
    /// <see cref="UnrecognizedTokens"/> (which only cares about the bool result) so both stay in
    /// sync by construction.</summary>
    private static bool TryRecognize(string token, WeaponProfile weapon, out WeaponProfile updated)
    {
        Match m;
        if ((m = AntiPattern().Match(token)).Success)
        {
            var anti = new Dictionary<string, int>(weapon.Anti)
            {
                [m.Groups[1].Value.Trim().ToLowerInvariant()] = int.Parse(m.Groups[2].Value)
            };
            updated = weapon with { Anti = anti };
            return true;
        }

        if ((m = MeltaPattern().Match(token)).Success)
        {
            updated = weapon with { Melta = int.Parse(m.Groups[1].Value) };
            return true;
        }

        if ((m = RapidFirePattern().Match(token)).Success)
        {
            updated = weapon with { RapidFire = int.Parse(m.Groups[1].Value) };
            return true;
        }

        if ((m = SustainedHitsPattern().Match(token)).Success)
        {
            updated = weapon with { SustainedHits = int.Parse(m.Groups[1].Value) };
            return true;
        }

        switch (token)
        {
            case "Torrent": updated = weapon with { Torrent = true }; return true;
            case "Blast": updated = weapon with { Blast = true }; return true;
            case "Lethal Hits": updated = weapon with { LethalHits = true }; return true;
            case "Devastating Wounds": updated = weapon with { DevastatingWounds = true }; return true;
            case "Twin-linked": updated = weapon with { TwinLinked = true }; return true;
            case "Indirect Fire": updated = weapon with { IndirectFire = true }; return true;
            case "Pistol": updated = weapon with { Pistol = true }; return true;
            case "Ignores Cover": updated = weapon with { IgnoresCover = true }; return true;
            case "Assault": updated = weapon with { Assault = true }; return true;
            default:
                updated = weapon; // unrecognized token: no flag set, verbatim text already retained
                return false;
        }
    }
}
