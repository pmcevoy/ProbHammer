namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Composite-pattern shape shared by Unit (leaf) and AttachedUnit (composite) so aggregate-view
/// logic can treat both uniformly - "an attached unit is a single unit for all rules purposes."
/// </summary>
public interface ICombatUnit
{
    /// <summary>The leaf Units that make up this combat unit. A plain Unit yields itself.</summary>
    IReadOnlyList<Unit> Components { get; }

    /// <summary>
    /// Computed display name, derived fresh from the current components' Datasheets on every read
    /// rather than stored - neither BattleScribe/NewRecruit exports nor GW's own app support
    /// free-form per-instance unit naming, so there is no external source a stored value could
    /// ever be populated from.
    /// </summary>
    string Name { get; }

    /// <summary>Player-set only, defaulting to false. Meaningful only when this combat unit's
    /// combined starting strength (see <see cref="HalfStrengthResolution"/>) is exactly 1, since
    /// the real at-or-below-half-strength determination for a single-model unit is wound-based and
    /// this app does not track partial wounds. For a combined starting strength of 2 or more, the
    /// computed determination governs instead and this value is not read for that purpose.</summary>
    bool IsHalfStrengthOverride { get; set; }

    /// <summary>Player-set only, defaulting to false - the app never simulates the 2D6-vs-
    /// Leadership Battle-shock test itself. Never cleared automatically by any other operation
    /// (a casualty adjustment, a half-strength change); only an explicit player action changes
    /// it.</summary>
    bool IsBattleShocked { get; set; }
}
