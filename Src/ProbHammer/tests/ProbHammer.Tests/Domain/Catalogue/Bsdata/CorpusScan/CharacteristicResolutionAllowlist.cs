using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="CharacteristicResolutionScanTests"/> - see
/// PROGRESS.md's Known Issues and design.md's "Confirmed by code inspection" decision. The
/// InSv representation gap was resolved by the `structured-invulnerable-save` change (every
/// confirmed real InSv shape now resolves into `InvulnerableSave` instead of throwing) except one
/// genuine data anomaly (Aeldari's Archon/Ynnari Archon); the dice-notation Strength gap remains
/// open separately, tracked here as before.
/// </summary>
public static partial class CharacteristicResolutionAllowlist
{
    public static IReadOnlyList<AllowlistEntry<Exception>> Entries { get; } =
    [
        new AllowlistEntry<Exception>(
            "Aeldari's Archon and Ynnari Archon both carry a footnoted InSv ('2+*') with no " +
            "matching ability anywhere in the entry - neither a locally-nested 'Invulnerable " +
            "Save ({digit}+*)'/'*Invulnerable Save' profile nor an InfoLink to one, under either " +
            "confirmed real-corpus naming convention. None of the entry's own abilities " +
            "(Leader/Overlord/Devious Mastermind/Hatred Eternal) mention an invulnerable save " +
            "either. A genuine BSData data anomaly (the footnote may reference a physical-card " +
            "note with no structured JSON equivalent), correctly resolved as unresolvable rather " +
            "than guessed at; see PROGRESS.md's Known Issues.",
            ex => ex is AmbiguousCharacteristicException { Characteristic: "InSv", Text: "2+*" }),

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
