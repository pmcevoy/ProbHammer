using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="CharacteristicResolutionScanTests"/> - see
/// PROGRESS.md's Known Issues. Every confirmed real InSv shape resolves into `InvulnerableSave`
/// instead of throwing except one genuine data anomaly (Aeldari's Archon/Ynnari Archon).
/// </summary>
public static class CharacteristicResolutionAllowlist
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
    ];
}
