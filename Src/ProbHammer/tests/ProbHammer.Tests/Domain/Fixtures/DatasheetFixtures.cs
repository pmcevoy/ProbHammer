using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Fixtures;

/// <summary>Hand-built static Datasheets used across domain-model tests. No BSData or parser dependency.</summary>
public static class DatasheetFixtures
{
    /// <summary>Single shared statline, referenced by every model-line (Assault Intercessor Squad-style).</summary>
    public static Datasheet AssaultIntercessorSquad()
    {
        var statline = new Statline(M: 6, T: 4, Sv: 3, W: 2, Ld: 6, Oc: 2);

        return new Datasheet(
            name: "Assault Intercessor Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY", "BATTLELINE", "ASSAULT INTERCESSOR SQUAD"],
            abilities: [new Ability { Name = "Shock Assault", Text = "...", Scope = AbilityScope.Unit }],
            statlines: new Dictionary<string, Statline> { ["Assault Intercessor"] = statline },
            weaponProfiles: [WeaponFixtures.ChainSword(), WeaponFixtures.HeavyBoltPistol()]
        );
    }

    /// <summary>Five distinct model types, each with its own statline (Chaos Space Marine-style).</summary>
    public static Datasheet ChaosSpaceMarineSquad()
    {
        var statlines = new Dictionary<string, Statline>
        {
            ["Chaos Space Marine"] = new(M: 6, T: 4, Sv: 3, W: 2, Ld: 7, Oc: 2),
            ["Chaos Space Marine Champion"] = new(M: 6, T: 4, Sv: 3, W: 3, Ld: 6, Oc: 2),
            ["Icon Bearer"] = new(M: 6, T: 4, Sv: 3, W: 2, Ld: 7, Oc: 2),
            ["Chaos Space Marine Gunner"] = new(M: 6, T: 4, Sv: 3, W: 3, Ld: 7, Oc: 2),
            ["Chaos Space Marine Reaper"] = new(M: 6, T: 4, Sv: 3, W: 3, Ld: 7, Oc: 2)
        };

        return new Datasheet(
            name: "Chaos Space Marines",
            factionKeywords: ["CHAOS", "HERETIC ASTARTES"],
            keywords: ["INFANTRY", "BATTLELINE"],
            abilities: [],
            statlines: statlines,
            weaponProfiles: [WeaponFixtures.ChainSword(), WeaponFixtures.BoltPistol()]
        );
    }

    /// <summary>
    ///     Shared statline referenced by both rank-and-file and Sergeant model-lines, with a
    ///     weapon profile (Master-crafted Power Weapon) only the Sergeant's model-line resolves.
    /// </summary>
    public static Datasheet CrusaderSquad()
    {
        var statline = new Statline(M: 6, T: 4, Sv: 3, W: 2, Ld: 6, Oc: 2);

        return new Datasheet(
            name: "Crusader Squad",
            factionKeywords: ["ADEPTUS ASTARTES", "BLACK TEMPLARS"],
            keywords: ["INFANTRY", "BATTLELINE"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Initiate"] = statline },
            [WeaponFixtures.PowerFist(), WeaponFixtures.MasterCraftedPowerWeapon()]
        );
    }
}
