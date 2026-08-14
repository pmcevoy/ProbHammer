using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Core.Domain.Examples;

public static class View
{
    /// <summary>The raw, unaggregated roster - a fresh <see cref="ICombatUnit"/> object graph every
    /// call, in the same fixed order <see cref="MyArmy"/> aggregates. Exposed separately so a
    /// caller can mutate <see cref="ModelLine"/> remaining counts (e.g. casualty tracking) before
    /// aggregating, without duplicating this six-unit list in two places.</summary>
    public static List<ICombatUnit> MyArmyRoster() =>
        [
            Units.ScoutSquad(),
            Units.AssaultIntercessorSquad(),
            Units.CrusaderSquad_Helbrecht_Ancient(),
            Units.CrusaderSquad_Marshal_Lieutenant(),
            Units.Impulsor(),
            Units.SwordBretheren_Marshal()
        ];

    public static List<AttachedUnitAggregateView> MyArmy() =>
        MyArmyRoster().Select(AttachedUnitAggregator.Build).ToList();
}