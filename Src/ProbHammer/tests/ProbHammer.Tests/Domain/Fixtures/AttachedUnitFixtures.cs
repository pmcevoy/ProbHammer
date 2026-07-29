using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class AttachedUnitFixtures
{
    public static Datasheet LeaderDatasheet(string name = "Chaplain")
    {
        var statline = new Statline(M: 6, T: 5, Sv: 3, W: 4, Ld: 6, Oc: 1);

        return new Datasheet(
            name: name,
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY", "IMPERIUM", name.ToUpperInvariant()],
            abilities: [],
            statlines: new Dictionary<string, Statline> { [name] = statline },
            weaponProfiles: []);
    }

    public static Datasheet SupportDatasheet()
    {
        var statline = new Statline(M: 6, T: 3, Sv: 4, W: 3, Ld: 7, Oc: 1);

        return new Datasheet(
            name: "Servitor",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY", "SERVITOR"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Servitor"] = statline },
            weaponProfiles: []);
    }

    public static Unit LeaderUnit(string name = "Chaplain") =>
        new(LeaderDatasheet(name), enhancements: [], modelLines: [new ModelLine(name, [], count: 1)]);

    public static Unit SupportUnit() =>
        new(SupportDatasheet(), enhancements: [], modelLines: [new ModelLine("Servitor", [], count: 1)]);

    /// <summary>Default one Leader, one Support attached to a Crusader Squad Bodyguard.</summary>
    public static AttachedUnit DefaultAttachedUnit() =>
        new(UnitFixtures.CrusaderSquadMixedLoadout(), [LeaderUnit(), SupportUnit()]);

    /// <summary>Documented exception: two Leaders plus one Support.</summary>
    public static AttachedUnit TwoLeadersPlusSupport() =>
        new(UnitFixtures.CrusaderSquadMixedLoadout(), [LeaderUnit("Chaplain"), LeaderUnit("Judiciar"), SupportUnit()]);

    /// <summary>Bodyguard fully destroyed; Leader/Support remain present.</summary>
    public static AttachedUnit BodyguardDestroyed()
    {
        var bodyguard = UnitFixtures.CrusaderSquadMixedLoadout();
        foreach (var modelLine in bodyguard.ModelLines)
            modelLine.RemoveCasualties(modelLine.Count);

        return new AttachedUnit(bodyguard, [LeaderUnit(), SupportUnit()]);
    }
}
