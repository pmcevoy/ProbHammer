namespace ProbHammer.Core.Domain.Catalogue;

public sealed record RangedWeapon(
    string Name,
    int Range,
    DiceExpression A,
    int Bs,
    int S,
    int Ap,
    DiceExpression D) : WeaponProfile(Name, WeaponType.Ranged, Range, A, S, Ap, D)
{
    public override int Skill => Bs;
}

public sealed record MeleeWeapon(
    string Name,
    DiceExpression A,
    int Ws,
    int S,
    int Ap,
    DiceExpression D) : WeaponProfile(Name, WeaponType.Melee, 0, A, S, Ap, D)
{
    public override int Skill => Ws;
}

/// <summary>Abstract base with sealed <see cref="RangedWeapon"/>/<see cref="MeleeWeapon"/>
/// subtypes rather than one record with a Type field, so a weapon's Type and Range can never
/// disagree with its own shape (a melee weapon can't accidentally carry a non-zero range).</summary>
public abstract record WeaponProfile(
    string Name,
    WeaponType Type,
    int Range,
    DiceExpression A,
    int S,
    int Ap,
    DiceExpression D)
{
    /// <summary>Computed per subtype (<see cref="RangedWeapon.Skill"/> => Bs, <see
    /// cref="MeleeWeapon.Skill"/> => Ws) rather than a stored/init value here, so Bs/Ws stay the
    /// single source of truth per subtype while shared logic (<see cref="EqualityKey"/>) reads
    /// one name. An earlier draft stored Skill separately, forwarded from Bs/Ws at construction -
    /// that let the two backing values desync under a <c>with</c> expression; computing Skill
    /// from the subtype's own field removes that failure mode structurally.</summary>
    public abstract int Skill { get; }

    public bool Torrent { get; init; }
    public bool Blast { get; init; }
    public int Melta { get; init; } // 0 = absent
    public int RapidFire { get; init; } // 0 = absent
    public int SustainedHits { get; init; } // 0 = absent
    public bool LethalHits { get; init; }
    public bool DevastatingWounds { get; init; }
    public bool TwinLinked { get; init; }
    public bool IndirectFire { get; init; }
    public bool Pistol { get; init; }
    public bool IgnoresCover { get; init; }
    public bool Assault { get; init; }
    public IReadOnlyDictionary<string, int> Anti { get; init; } = new Dictionary<string, int>();

    /// <summary>
    ///     Exact source keyword text, in source order, independent of whether any individual
    ///     keyword also corresponds to one of this profile's typed ability flags above.
    ///     Rendering of a weapon's keywords must read this verbatim record rather than being
    ///     reconstructed from the typed flags - see datasheet-catalogue's "Weapon Profile
    ///     Verbatim Keyword Text" requirement for why (an unrecognized token, or a
    ///     context-dependent alternate spelling of an already-modeled flag, must never be
    ///     silently dropped or rendered under the flag's own canonical wording instead).
    /// </summary>
    public IReadOnlyList<string> KeywordsText { get; init; } = [];

    /// <summary>
    ///     Structural equality for aggregation purposes: (Type, Skill, Strength, Ap, Damage, and
    ///     every ability/keyword flag). Excludes Name/Range/Attacks - count/attacks are the
    ///     quantity being aggregated, not part of the profile's identity. Mirrors
    ///     SimulationAdapter.WeaponGroupKey. Every ability property on WeaponProfile must appear
    ///     here - two weapons differing in any one keyword (e.g. Pistol vs Assault) are different
    ///     profiles and must not be merged, even if their damage-relevant stats coincide.
    /// </summary>
    public WeaponProfileEqualityKey EqualityKey()
    {
        return new WeaponProfileEqualityKey(
            Type, Skill, S, Ap, D,
            Torrent, Blast, Melta, RapidFire,
            SustainedHits, LethalHits, DevastatingWounds,
            TwinLinked, IndirectFire, Pistol, IgnoresCover, Assault,
            NormaliseAnti(Anti),
            string.Join("", KeywordsText));
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
    DiceExpression D,
    bool Torrent,
    bool Blast,
    int Melta,
    int RapidFire,
    int SustainedHits,
    bool LethalHits,
    bool DevastatingWounds,
    bool TwinLinked,
    bool IndirectFire,
    bool Pistol,
    bool IgnoresCover,
    bool Assault,
    string Anti,
    string KeywordsText);