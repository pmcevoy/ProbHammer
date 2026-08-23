namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Domain-level computation of 11th edition's "at or below half-strength" determination (the
/// trigger for a Battle-shock test). Not wired to Simulation/* - matches ToughnessResolution's own
/// scope note.
/// </summary>
public static class HalfStrengthResolution
{
    /// <summary>Combined starting strength across every component - for an AttachedUnit this sums
    /// the Bodyguard and every Attached unit together, matching the rule that an attached unit's
    /// starting strength is the number of models it contains at the start of the first battle
    /// round.</summary>
    public static int StartingStrength(ICombatUnit unit) =>
        unit.Components.Sum(c => c.ModelLines.Sum(ml => ml.Count));

    /// <summary>Combined current strength - the live counterpart to <see cref="StartingStrength"/>.</summary>
    public static int CurrentStrength(ICombatUnit unit) =>
        unit.Components.Sum(c => c.ModelLines.Sum(ml => ml.RemainingCount));

    /// <summary>True when current strength is at or below half of starting strength, rounded
    /// down (e.g. a 5-model unit's threshold is 2, not 3 - it takes 3 casualties, not 2, to
    /// reach it). Only meaningful when <see cref="StartingStrength"/> is 2 or more - a combined
    /// starting strength of exactly 1 is wound-based in the real rule, which this app cannot
    /// compute (see <see cref="ICombatUnit.IsHalfStrengthOverride"/>), so this always returns
    /// false in that case rather than a misleading model-count answer.</summary>
    public static bool IsAtOrBelowHalfStrength(ICombatUnit unit)
    {
        var starting = StartingStrength(unit);
        if (starting < 2)
            return false;

        var half = starting / 2; // integer division floors for positive operands
        return CurrentStrength(unit) <= half;
    }

    /// <summary>The one at-or-below-half-strength status a caller needs, combining the computed
    /// determination (starting strength >= 2) with the player-set override (starting strength ==
    /// 1) without leaking which of the two produced it.</summary>
    public static bool IsAtOrBelowHalfStrengthStatus(ICombatUnit unit) =>
        StartingStrength(unit) == 1 ? unit.IsHalfStrengthOverride : IsAtOrBelowHalfStrength(unit);
}
