using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class AggregateViewFixtures
{
    /// <summary>4 Bodyguard models carrying "Astartes chainsword" and an attached Leader carrying
    /// a differently-named "Master-crafted chainsword" with an identical structural profile -
    /// proves aggregation groups by profile equality, not by weapon name.</summary>
    public static AttachedUnit WeaponAggregationAttachedUnit()
    {
        var bodyguardDatasheet = DatasheetFixtures.AssaultIntercessorSquad();
        var bodyguard = new Unit(
            bodyguardDatasheet, [],
            [new ModelLine("Assault Intercessor", [WeaponFixtures.ChainSword.Name], count: 4)]);

        var leaderDatasheet = new Datasheet(
            name: "Chaplain",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Chaplain"] = new(6, 4, 3, 4, 6, 1) },
            weaponProfiles: new Dictionary<string, WeaponProfile>
            {
                [WeaponFixtures.ChainSword.Name] = WeaponFixtures.ChainSword
            });
        var leader = new Unit(
            leaderDatasheet, [],
            [new ModelLine("Chaplain", [WeaponFixtures.ChainSword.Name], count: 1)]);

        return new AttachedUnit(bodyguard, [leader]);
    }

    /// <summary>Bodyguard with a Unit-scoped intrinsic ability, and an attached Leader whose
    /// model-line carries a Model-scoped ability (as if granted by an Enhancement).</summary>
    public static AttachedUnit ModelScopedAbilityAttachedUnit()
    {
        var bodyguardDatasheet = new Datasheet(
            name: "Crusader Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY", "BATTLELINE"],
            abilities: [new Ability { Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit }],
            statlines: new Dictionary<string, Statline> { ["Initiate"] = new(6, 4, 3, 2, 6, 2) },
            weaponProfiles: new Dictionary<string, WeaponProfile>());
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Initiate", [], count: 5)]);

        var leaderDatasheet = new Datasheet(
            name: "Chaplain",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Chaplain"] = new(6, 4, 3, 4, 6, 1) },
            weaponProfiles: new Dictionary<string, WeaponProfile>());

        var ironHalo = new Ability { Name = "Iron Halo", Text = "This model has a 4+ invulnerable save.", Scope = AbilityScope.Model };
        var leaderLine = new ModelLine("Chaplain", [], count: 1, abilities: [ironHalo]);
        var leader = new Unit(leaderDatasheet, ["Iron Halo"], [leaderLine]);

        return new AttachedUnit(bodyguard, [leader]);
    }
}
