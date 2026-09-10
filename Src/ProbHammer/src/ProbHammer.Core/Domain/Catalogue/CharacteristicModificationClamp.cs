namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Enforces a characteristic's own rulebook-legal value bound after a delta or `Set` value
/// is resolved. A separate lookup table from <see cref="CharacteristicModificationKinds"/>, keyed by
/// the same characteristic names, since a characteristic's arithmetic family and its clamp bound
/// are independent facts (WS and Ld share RollThreshold but have different bounds).</summary>
public static class CharacteristicModificationClamp
{
    private sealed record Bound(int? Floor, int? Ceiling);

    private static readonly Dictionary<string, Bound> Bounds =
        new()
        {
            ["Sv"] = new Bound(Floor: 2, Ceiling: null),
            ["Ld"] = new Bound(Floor: 5, Ceiling: 8),
            ["WS"] = new Bound(Floor: 2, Ceiling: 6),
            ["BS"] = new Bound(Floor: 2, Ceiling: 6),
            ["Oc"] = new Bound(Floor: 0, Ceiling: null),
            // AP is stored as a negative integer (root CLAUDE.md) - "cannot be worse than 0" means
            // it can never resolve positive, i.e. a ceiling of 0 with no floor.
            ["AP"] = new Bound(Floor: null, Ceiling: 0),
            ["M"] = new Bound(Floor: 1, Ceiling: null),
            ["T"] = new Bound(Floor: 1, Ceiling: null),
            ["S"] = new Bound(Floor: 1, Ceiling: null),
            ["Range"] = new Bound(Floor: 1, Ceiling: null),
            // A fixed (non-dice-rolling) Damage value clamps through this same table via
            // ApplyToDice's own Count == 0 branch below - see that method's own doc comment.
            ["D"] = new Bound(Floor: 1, Ceiling: null)
        };

    /// <summary>Silently caps rather than throwing - a resolved value outside its legal bound is an
    /// expected outcome of stacking effects, not an error condition. A characteristic with no
    /// registered bound (e.g. W, which the rulebook table places no explicit limit on beyond staying
    /// a valid characteristic) passes through unchanged.</summary>
    public static int Apply(string characteristic, int value)
    {
        if (!Bounds.TryGetValue(characteristic, out var bound))
            return value;

        if (bound.Floor is { } floor && value < floor) value = floor;
        if (bound.Ceiling is { } ceiling && value > ceiling) value = ceiling;
        return value;
    }

    /// <summary>The dice-shaped counterpart to <see cref="Apply"/>, for Damage
    /// (`classify-weapon-characteristic-effects` design.md's "Damage's clamp floor" decision) - not
    /// a lookup against <see cref="Bounds"/> (that table assumes its value IS the resolved value
    /// itself, not a derived quantity), since a dice value's own legal bound is enforced against its
    /// GUARANTEED MINIMUM (<c>Count + Modifier</c> when it rolls one or more dice, else the bare
    /// <c>Modifier</c>), not its literal <see cref="DiceExpression.Modifier"/> value directly. A
    /// fixed (<c>Count == 0</c>) value clamps exactly like <see cref="Apply"/> already does for any
    /// other Plain-family scalar (floor of 1, mirroring M/T/S/Range). A dice-rolling value whose
    /// guaranteed minimum would fall below 1 has its <see cref="DiceExpression.Modifier"/> raised
    /// just enough to bring the guaranteed minimum back to 1 - e.g. "D6" worsened by 8 clamps to a
    /// modifier of -5 ("D6-5", guaranteed minimum 1), not "D6-8" (guaranteed minimum -7).</summary>
    public static DiceExpression ApplyToDice(string characteristic, DiceExpression value)
    {
        if (characteristic != "D")
            return value;

        if (value.Count == 0)
            return DiceExpression.Fixed(Apply(characteristic, value.Modifier));

        var guaranteedMinimum = value.Count + value.Modifier;
        return guaranteedMinimum < 1
            ? value with { Modifier = value.Modifier + (1 - guaranteedMinimum) }
            : value;
    }
}