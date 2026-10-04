namespace ProbHammer.Core.Domain.Catalogue;

public sealed record RangedWeapon(
    string Name,
    int Range,
    DiceExpression A,
    ScalarCharacteristicView Bs,
    ScalarCharacteristicView S,
    ScalarCharacteristicView Ap,
    ScalarCharacteristicView D) : WeaponProfile(Name, WeaponType.Ranged, Range, A, S, Ap, D)
{
    public override ScalarCharacteristicView Skill => Bs;
}

public sealed record MeleeWeapon(
    string Name,
    DiceExpression A,
    ScalarCharacteristicView Ws,
    ScalarCharacteristicView S,
    ScalarCharacteristicView Ap,
    ScalarCharacteristicView D) : WeaponProfile(Name, WeaponType.Melee, 0, A, S, Ap, D)
{
    public override ScalarCharacteristicView Skill => Ws;
}

/// <summary>Abstract base with sealed <see cref="RangedWeapon"/>/<see cref="MeleeWeapon"/>
/// subtypes rather than one record with a Type field, so a weapon's Type and Range can never
/// disagree with its own shape (a melee weapon can't accidentally carry a non-zero range).</summary>
public abstract record WeaponProfile(
    string Name,
    WeaponType Type,
    int Range,
    DiceExpression A,
    ScalarCharacteristicView S,
    ScalarCharacteristicView Ap,
    ScalarCharacteristicView D)
{
    /// <summary>Computed per subtype (<see cref="RangedWeapon.Skill"/> => Bs, <see
    /// cref="MeleeWeapon.Skill"/> => Ws) rather than a stored/init value here, so Bs/Ws stay the
    /// single source of truth per subtype while shared logic (<see cref="EqualityKey"/>) reads
    /// one name. Storing Skill separately, forwarded from Bs/Ws at construction, would let the two
    /// backing values desync under a <c>with</c> expression; computing Skill from the subtype's
    /// own field removes that failure mode structurally.</summary>
    public abstract ScalarCharacteristicView Skill { get; }

    /// <summary>
    ///     Exact source keyword text, in source order. The sole representation of a weapon's
    ///     ability keywords - rendering must read this verbatim record so an unrecognized or
    ///     newly-added BSData keyword is never silently dropped.
    /// </summary>
    public IReadOnlyList<string> KeywordsText { get; init; } = [];

    /// <summary>
    ///     Structural equality for aggregation purposes: (Type, Skill, Strength, Ap, Damage, and
    ///     a normalized keyword set). Excludes Name/Range/Attacks - count/attacks are the
    ///     quantity being aggregated, not part of the profile's identity. Mirrors
    ///     SimulationAdapter.WeaponGroupKey. Two weapons differing in any real keyword are
    ///     different profiles and must not be merged, even if their damage-relevant stats
    ///     coincide.
    /// </summary>
    public WeaponProfileEqualityKey EqualityKey()
    {
        return new WeaponProfileEqualityKey(
            Type, Skill, S, Ap, D,
            NormaliseKeywords(KeywordsText));
    }

    /// <summary>
    ///     Deduplicated, sorted-then-joined <see cref="WeaponKeyword"/> identity plus value of each
    ///     <see cref="KeywordsText"/> token - order and casing differences must not split what should
    ///     be one aggregated entry, while a different value or Anti target still must.
    /// </summary>
    private static string NormaliseKeywords(IReadOnlyList<string> keywordsText)
    {
        var normalized = keywordsText
            .Select(WeaponKeyword.Parse)
            .Where(keyword => keyword.Identity.Length > 0)
            .Select(keyword => $"{keyword.Identity}={keyword.Value}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(token => token, StringComparer.Ordinal);
        return string.Join(",", normalized);
    }
}

public sealed record WeaponProfileEqualityKey(
    WeaponType WeaponType,
    ScalarCharacteristicView Skill,
    ScalarCharacteristicView S,
    ScalarCharacteristicView Ap,
    ScalarCharacteristicView D,
    string Keywords);