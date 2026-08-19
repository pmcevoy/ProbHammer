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

        var anti = new Dictionary<string, int>(weapon.Anti);

        foreach (var token in tokens)
        {
            Match m;
            if ((m = AntiPattern().Match(token)).Success)
            {
                anti[m.Groups[1].Value.Trim().ToLowerInvariant()] = int.Parse(m.Groups[2].Value);
            }
            else if ((m = MeltaPattern().Match(token)).Success)
            {
                weapon = weapon with { Melta = int.Parse(m.Groups[1].Value) };
            }
            else if ((m = RapidFirePattern().Match(token)).Success)
            {
                weapon = weapon with { RapidFire = int.Parse(m.Groups[1].Value) };
            }
            else if ((m = SustainedHitsPattern().Match(token)).Success)
            {
                weapon = weapon with { SustainedHits = int.Parse(m.Groups[1].Value) };
            }
            else
            {
                weapon = token switch
                {
                    "Torrent" => weapon with { Torrent = true },
                    "Blast" => weapon with { Blast = true },
                    "Lethal Hits" => weapon with { LethalHits = true },
                    "Devastating Wounds" => weapon with { DevastatingWounds = true },
                    "Twin-linked" => weapon with { TwinLinked = true },
                    "Indirect Fire" => weapon with { IndirectFire = true },
                    "Pistol" => weapon with { Pistol = true },
                    "Ignores Cover" => weapon with { IgnoresCover = true },
                    "Assault" => weapon with { Assault = true },
                    _ => weapon // unrecognized token: no flag set, verbatim text already retained
                };
            }
        }

        return weapon with { Anti = anti };
    }
}
