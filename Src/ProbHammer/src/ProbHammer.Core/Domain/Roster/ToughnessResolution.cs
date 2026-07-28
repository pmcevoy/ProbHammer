namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Domain-level computation of the Toughness resolution rule for attacks against an AttachedUnit.
/// Not wired to Simulation/* in this change - Simulation is paused pending a later reconciliation.
/// </summary>
public static class ToughnessResolution
{
    /// <summary>Highest Toughness among the Bodyguard's currently-present models if any remain,
    /// otherwise the highest Toughness among the currently-present Leader/Support models.</summary>
    public static int ResolveDefendingToughness(AttachedUnit attachedUnit)
    {
        var bodyguardToughnesses = PresentToughnesses(attachedUnit.Bodyguard).ToList();
        if (bodyguardToughnesses.Count > 0)
            return bodyguardToughnesses.Max();

        var attachedToughnesses = attachedUnit.Attached.SelectMany(PresentToughnesses).ToList();
        if (attachedToughnesses.Count == 0)
            throw new InvalidOperationException(
                "AttachedUnit has no present models to resolve a Toughness characteristic against.");

        return attachedToughnesses.Max();
    }

    private static IEnumerable<int> PresentToughnesses(Unit unit) =>
        unit.ModelLines
            .Where(ml => ml.RemainingCount > 0)
            .Select(ml => unit.Datasheet.GetStatline(ml.StatlineName).Toughness);
}
