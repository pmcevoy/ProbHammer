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
}
