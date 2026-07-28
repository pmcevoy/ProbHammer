namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Composite-pattern shape shared by Unit (leaf) and AttachedUnit (composite) so aggregate-view
/// logic can treat both uniformly - "an attached unit is a single unit for all rules purposes."
/// </summary>
public interface ICombatUnit
{
    /// <summary>The leaf Units that make up this combat unit. A plain Unit yields itself.</summary>
    IReadOnlyList<Unit> Components { get; }
}
