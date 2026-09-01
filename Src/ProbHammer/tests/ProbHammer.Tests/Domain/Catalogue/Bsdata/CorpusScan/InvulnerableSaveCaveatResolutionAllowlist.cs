namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="InvulnerableSaveCaveatResolutionScanTests"/> -
/// every real still-caveated InSv result left in the live corpus after
/// <see cref="ProbHammer.Core.Domain.Catalogue.InvulnerableSaveCaveatClassifier"/>'s known-template
/// match, confirmed by inspection to be a genuine restriction the closed vocabulary correctly
/// declines to resolve (not a missed template).
/// </summary>
public static class InvulnerableSaveCaveatResolutionAllowlist
{
    public static IReadOnlyList<AllowlistEntry<InvulnerableSaveCaveatResolutionScanTests.StillCaveatedResult>>
        Entries { get; } =
    [
        new AllowlistEntry<InvulnerableSaveCaveatResolutionScanTests.StillCaveatedResult>(
            "Orks' Makari (via Ghazghkull Thraka) - the confirmed real anomaly proposal.md itself " +
            "calls out: the linked ability restricts re-rolling save rolls, not an attack-type " +
            "split, so it correctly doesn't match any known template and stays caveated.",
            r => r.FileName == "Orks.json" && r.EntryName == "Ghazghkull Thraka" &&
                 r.StatlineName == "Makari" && r.AbilityText.Contains("cannot re")),
    ];
}