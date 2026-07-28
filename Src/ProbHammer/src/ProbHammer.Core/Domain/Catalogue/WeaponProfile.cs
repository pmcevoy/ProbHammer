namespace ProbHammer.Core.Domain.Catalogue;

public sealed record WeaponProfile
{
    public required string Name { get; init; }
    public required WeaponType Type { get; init; }
    public required int Range { get; init; }     // inches; melee weapons use 0
    public required int Attacks { get; init; }
    public required int Skill { get; init; }
    public required int Strength { get; init; }
    public required int Ap { get; init; }         // negative integer matching game value (e.g. AP-2 -> -2)
    public required int Damage { get; init; }
    public required WeaponAbilities Abilities { get; init; }

    /// <summary>
    /// Structural equality for aggregation purposes: (Type, Skill, Strength, Ap, Damage, Abilities).
    /// Excludes Name/Range/Attacks - count/attacks are the quantity being aggregated, not part of
    /// the profile's identity. Mirrors SimulationAdapter.WeaponGroupKey.
    /// </summary>
    public WeaponProfileEqualityKey EqualityKey() => new(
        Type, Skill, Strength, Ap, Damage,
        Abilities.Torrent, Abilities.Blast, Abilities.Melta, Abilities.RapidFire,
        Abilities.SustainedHits, Abilities.LethalHits, Abilities.DevastatingWounds,
        Abilities.TwinLinked, Abilities.IndirectFire, NormaliseAnti(Abilities.Anti));

    private static string NormaliseAnti(IReadOnlyDictionary<string, int> anti) =>
        anti.Count == 0
            ? ""
            : string.Join(",", anti.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key}:{kv.Value}"));
}

public sealed record WeaponProfileEqualityKey(
    WeaponType Type,
    int Skill,
    int Strength,
    int Ap,
    int Damage,
    bool Torrent,
    bool Blast,
    int Melta,
    int RapidFire,
    int SustainedHits,
    bool LethalHits,
    bool DevastatingWounds,
    bool TwinLinked,
    bool IndirectFire,
    string Anti);
