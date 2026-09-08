namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Enforces a characteristic's own rulebook-legal value bound after a delta or `Set` value
/// is resolved - see introduce-characteristic-modification-kind/design.md's "Per-characteristic
/// clamp bound" table, transcribed verbatim from the user in .claude/vnext-ideas.md. A separate
/// lookup table from <see cref="CharacteristicModificationKinds"/>, keyed by the same characteristic
/// names, since a characteristic's arithmetic family and its clamp bound are independent facts (WS
/// and Ld share RollThreshold but have different bounds).</summary>
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
            ["Range"] = new Bound(Floor: 1, Ceiling: null)
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
}
