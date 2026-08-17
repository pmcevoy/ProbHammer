using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Core.Domain.Examples;

public static class View
{
    /// <summary>The full army list - the same six-unit roster <see cref="MyArmyRoster"/> and
    /// <see cref="MyArmy"/> read from, wrapped with the army-level metadata a real GW app export
    /// carries. Sample values transcribed from <c>data/gw-app-export.txt</c>.</summary>
    public static ArmyRoster Roster() =>
        new(
            name: "For the Emperor",
            pointsSpent: 1065,
            faction: ["Space Marines", "Black Templars"],
            detachments: ["Companions of Vehemence"],
            forceDisposition: "Purge the Foe",
            battleSize: "Incursion",
            pointsLimit: 1000,
            units:
            [
                Units.ScoutSquad(),
                Units.AssaultIntercessorSquad(),
                Units.CrusaderSquad_Helbrecht_Ancient(),
                Units.CrusaderSquad_Marshal_Lieutenant(),
                Units.Impulsor(),
                Units.SwordBretheren_Marshal()
            ]);

    /// <summary>The raw, unaggregated roster - a fresh <see cref="ICombatUnit"/> object graph every
    /// call, in the same fixed order <see cref="MyArmy"/> aggregates. Exposed separately so a
    /// caller can mutate <see cref="ModelLine"/> remaining counts (e.g. casualty tracking) before
    /// aggregating, without duplicating this six-unit list in two places.</summary>
    public static List<ICombatUnit> MyArmyRoster() => Roster().Units.ToList();

    public static List<AttachedUnitAggregateView> MyArmy() =>
        MyArmyRoster().Select(AttachedUnitAggregator.Build).ToList();
}