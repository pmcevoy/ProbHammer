namespace ProbHammer.Core.Domain.Roster;

/// <summary>An army list's per-unit roster together with the army-level metadata no individual
/// Unit or AttachedUnit carries.</summary>
public sealed class ArmyRoster
{
    public string Name { get; }
    public int PointsSpent { get; }

    /// <summary>Ordered: a parent codex before its sub-faction (e.g. ["Space Marines", "Black
    /// Templars"]), or a single entry when the faction has no sub-faction split (e.g. ["Chaos
    /// Space Marines"]).</summary>
    public IReadOnlyList<string> Faction { get; }

    /// <summary>Ordered by selection, one entry per detachment chosen (a battle size's detachment
    /// points may buy more than one).</summary>
    public IReadOnlyList<string> Detachments { get; }

    public string ForceDisposition { get; }
    public string BattleSize { get; }
    public int PointsLimit { get; }
    public IReadOnlyList<ICombatUnit> Units { get; }

    public ArmyRoster(
        string name,
        int pointsSpent,
        IEnumerable<string> faction,
        IEnumerable<string> detachments,
        string forceDisposition,
        string battleSize,
        int pointsLimit,
        IEnumerable<ICombatUnit> units)
    {
        Name = name;
        PointsSpent = pointsSpent;
        Faction = faction.ToList();
        Detachments = detachments.ToList();
        ForceDisposition = forceDisposition;
        BattleSize = battleSize;
        PointsLimit = pointsLimit;
        Units = units.ToList();
    }
}
