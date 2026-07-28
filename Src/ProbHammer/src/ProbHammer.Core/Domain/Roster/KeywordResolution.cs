namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Pure function over "currently present" components - never stored state, so it can't go stale
/// when a component is destroyed.
/// </summary>
public static class KeywordResolution
{
    /// <summary>The union of Keywords of whichever component Units are currently present, for
    /// unit-level rule checks against a Unit or AttachedUnit.</summary>
    public static IReadOnlySet<string> EffectiveKeywords(ICombatUnit combatUnit) =>
        combatUnit.Components
            .Where(u => u.IsPresent)
            .SelectMany(u => u.Datasheet.Keywords)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
