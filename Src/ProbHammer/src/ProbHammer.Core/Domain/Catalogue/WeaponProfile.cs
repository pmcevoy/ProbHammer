namespace ProbHammer.Core.Domain.Catalogue;

public sealed record RangedWeapon(
    string Name,
    int Range,
    int A,
    int Bs,
    int S,
    int Ap,
    int D) : WeaponProfile(Name, WeaponType.Ranged, Range, A, S, Ap, D)
{
    public override  int Skill => Bs;
}

public sealed record MeleeWeapon(
    string Name,
    int A,
    int Ws,
    int S,
    int Ap,
    int D) : WeaponProfile(Name, WeaponType.Melee, 0, A, S, Ap, D)
{
    public override int Skill => Ws;
}

public abstract record WeaponProfile(
    string Name,
    WeaponType Type,
    int Range,
    int A, int S, int Ap, int D)
{
    public abstract int Skill { get; }
    public bool Torrent { get; init; }
    public bool Blast { get; init; }
    public int Melta { get; init; }          // 0 = absent
    public int RapidFire { get; init; }      // 0 = absent
    public int SustainedHits { get; init; }  // 0 = absent
    public bool LethalHits { get; init; }
    public bool DevastatingWounds { get; init; }
    public bool TwinLinked { get; init; }
    public bool IndirectFire { get; init; }
    public bool Pistol { get; init; }
    public IReadOnlyDictionary<string, int> Anti { get; init; } = new Dictionary<string, int>();
    /// <summary>
    ///     Structural equality for aggregation purposes: (Type, Skill, Strength, Ap, Damage, Abilities).
    ///     Excludes Name/Range/Attacks - count/attacks are the quantity being aggregated, not part of
    ///     the profile's identity. Mirrors SimulationAdapter.WeaponGroupKey.
    /// </summary>
    public WeaponProfileEqualityKey EqualityKey()
    {
        return new WeaponProfileEqualityKey(
            Type, Skill, S, Ap, D,
            Torrent, Blast, Melta, RapidFire,
            SustainedHits, LethalHits, DevastatingWounds,
            TwinLinked, IndirectFire, NormaliseAnti(Anti));
    }

    private static string NormaliseAnti(IReadOnlyDictionary<string, int> anti)
    {
        return anti.Count == 0
            ? ""
            : string.Join(",", anti.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}

public sealed record WeaponProfileEqualityKey(
    WeaponType WeaponType,
    int Skill,
    int S,
    int Ap,
    int D,
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
    