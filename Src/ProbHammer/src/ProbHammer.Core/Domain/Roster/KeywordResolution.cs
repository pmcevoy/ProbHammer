namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Pure function over "currently present" components - never stored state, so it can't go stale
/// when a component is destroyed.
/// </summary>
public static class KeywordResolution
{
    /// <summary>The union of Keywords of whichever component Units are currently present, plus
    /// the Keywords of whichever of those components' model-lines are currently present
    /// (RemainingCount > 0), for unit-level rule checks against a Unit or AttachedUnit.</summary>
    public static IReadOnlySet<string> EffectiveKeywords(ICombatUnit combatUnit)
    {
        var presentComponents = combatUnit.Components.Where(u => u.IsPresent).ToList();

        var datasheetKeywords = presentComponents.SelectMany(u => u.Datasheet.Keywords);
        var modelLineKeywords = presentComponents
            .SelectMany(u => u.ModelLines)
            .Where(ml => ml.RemainingCount > 0)
            .SelectMany(ml => ml.Keywords);

        return datasheetKeywords.Concat(modelLineKeywords).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
