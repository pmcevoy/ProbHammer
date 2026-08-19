using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="CharacteristicResolutionScanTests"/> - see
/// PROGRESS.md's Known Issues and design.md's "Confirmed by code inspection" decision. Both
/// parked representation questions (multi-value InSv, dice-notation Strength) stay unresolved;
/// this only makes their resulting exceptions expected rather than unexplained failures.
/// </summary>
public static partial class CharacteristicResolutionAllowlist
{
    public static IReadOnlyList<AllowlistEntry<Exception>> Entries { get; } =
    [
        new AllowlistEntry<Exception>(
            "InSv caveated/multi-value form ParseThreshold rejects - Statline.InSv is a plain " +
            "int with no way to represent a conditional or multi-valued save. Covers all " +
            "confirmed real-data shapes: a '/'-split two-different-values-by-context form (e.g. " +
            "'4+* / 5+' on Wyches/Howling Banshees); a parenthetical annotation (e.g. " +
            "'5+ (Ranged)' on the four Titans + the Stormbird); and a bare trailing '*' footnote " +
            "(e.g. '5+*' on Aeldari Rangers and most Imperial/Chaos Knights) - the footnote form " +
            "is by far the most common in a full-corpus scan (~40 of ~50 InSv failures) and " +
            "usually links to a shared ability restricting the save to one attack type (ranged- " +
            "or melee-only), though at least one case (Ork's Makari) uses the same '*' for an " +
            "unrelated 'no re-rolls' caveat that doesn't condition the value itself - either way " +
            "the raw characteristic text alone can't be resolved without losing information, so " +
            "every InSv failure is treated as one class here; see PROGRESS.md's Known Issues.",
            ex => ex is AmbiguousCharacteristicException { Characteristic: "InSv" }),

        new AllowlistEntry<Exception>(
            "Dice-notation Strength characteristic (e.g. Ork Battlewagon 'D6+6', Big Gunz " +
            "[Legends] '2D6', Deff Rolla Battle Fortress [Legends] '2D6') fails ParsePlainInt's " +
            "int.Parse - WeaponProfile.S has no way to represent a dice expression; see " +
            "PROGRESS.md's Known Issues.",
            ex => ex is FormatException && DiceNotationInMessage().IsMatch(ex.Message)),
    ];

    [GeneratedRegex(@"'\d*D\d+")]
    private static partial Regex DiceNotationInMessage();
}
