using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Core.Domain.Examples;

public static class View
{
    public static List<AttachedUnitAggregateView> MyArmy()
    {
        var army = new List<AttachedUnitAggregateView>
        {
            AttachedUnitAggregator.Build(Units.ScoutSquad()),
            AttachedUnitAggregator.Build(Units.AssaultIntercessorSquad()),
            AttachedUnitAggregator.Build(Units.CrusaderSquad_Helbrecht_Ancient()),
            AttachedUnitAggregator.Build(Units.CrusaderSquad_Marshal_Lieutenant()),
            AttachedUnitAggregator.Build(Units.Impulsor()),
            AttachedUnitAggregator.Build(Units.SwordBretheren_Marshal())
        };

        return army;
    }
}