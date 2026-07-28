using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Fixtures;

/// <summary>Hand-built static Datasheets used across domain-model tests. No BSData or parser dependency.</summary>
public static class DatasheetFixtures
{
    /// <summary>Single shared statline, referenced by every model-line (Assault Intercessor Squad-style).</summary>
    public static Datasheet AssaultIntercessorSquad()
    {
        var statline = new Statline(Movement: 6, Toughness: 4, Save: 3, Wounds: 2, Leadership: 6, ObjectiveControl: 2);

        return new Datasheet(
            name: "Assault Intercessor Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY", "BATTLELINE", "ASSAULT INTERCESSOR SQUAD"],
            abilities: [new Ability { Name = "Shock Assault", Text = "...", Scope = AbilityScope.Unit }],
            statlines: new Dictionary<string, Statline> { ["Assault Intercessor"] = statline },
            weaponProfiles: new Dictionary<string, WeaponProfile>
            {
                ["Astartes chainsword"] = new()
                {
                    Name = "Astartes chainsword", Type = WeaponType.Melee, Range = 0, Attacks = 4,
                    Skill = 3, Strength = 4, Ap = -1, Damage = 1, Abilities = new WeaponAbilities()
                },
                ["Heavy bolt pistol"] = new()
                {
                    Name = "Heavy bolt pistol", Type = WeaponType.Ranged, Range = 12, Attacks = 1,
                    Skill = 3, Strength = 4, Ap = -1, Damage = 1, Abilities = new WeaponAbilities()
                }
            });
    }

    /// <summary>Five distinct model types, each with its own statline (Chaos Space Marine-style).</summary>
    public static Datasheet ChaosSpaceMarineSquad()
    {
        var statlines = new Dictionary<string, Statline>
        {
            ["Chaos Space Marine"] = new(Movement: 6, Toughness: 4, Save: 3, Wounds: 2, Leadership: 7, ObjectiveControl: 2),
            ["Chaos Space Marine Champion"] = new(Movement: 6, Toughness: 4, Save: 3, Wounds: 3, Leadership: 6, ObjectiveControl: 2),
            ["Icon Bearer"] = new(Movement: 6, Toughness: 4, Save: 3, Wounds: 2, Leadership: 7, ObjectiveControl: 2),
            ["Chaos Space Marine Gunner"] = new(Movement: 6, Toughness: 4, Save: 3, Wounds: 3, Leadership: 7, ObjectiveControl: 2),
            ["Chaos Space Marine Reaper"] = new(Movement: 6, Toughness: 4, Save: 3, Wounds: 3, Leadership: 7, ObjectiveControl: 2)
        };

        return new Datasheet(
            name: "Chaos Space Marines",
            factionKeywords: ["CHAOS", "HERETIC ASTARTES"],
            keywords: ["INFANTRY", "BATTLELINE"],
            abilities: [],
            statlines: statlines,
            weaponProfiles: new Dictionary<string, WeaponProfile>
            {
                ["Astartes chainsword"] = new()
                {
                    Name = "Astartes chainsword", Type = WeaponType.Melee, Range = 0, Attacks = 4,
                    Skill = 3, Strength = 4, Ap = -1, Damage = 1, Abilities = new WeaponAbilities()
                },
                ["Bolt pistol"] = new()
                {
                    Name = "Bolt pistol", Type = WeaponType.Ranged, Range = 12, Attacks = 1,
                    Skill = 3, Strength = 4, Ap = 0, Damage = 1, Abilities = new WeaponAbilities()
                }
            });
    }

    /// <summary>Shared statline referenced by both rank-and-file and Sergeant model-lines, with a
    /// weapon profile (Master-crafted Power Weapon) only the Sergeant's model-line resolves.</summary>
    public static Datasheet CrusaderSquad()
    {
        var statline = new Statline(Movement: 6, Toughness: 4, Save: 3, Wounds: 2, Leadership: 6, ObjectiveControl: 2);

        return new Datasheet(
            name: "Crusader Squad",
            factionKeywords: ["ADEPTUS ASTARTES", "BLACK TEMPLARS"],
            keywords: ["INFANTRY", "BATTLELINE"],
            abilities: [],
            statlines: new Dictionary<string, Statline> { ["Initiate"] = statline },
            weaponProfiles: new Dictionary<string, WeaponProfile>
            {
                ["Power Fist"] = new()
                {
                    Name = "Power Fist", Type = WeaponType.Melee, Range = 0, Attacks = 2,
                    Skill = 3, Strength = 8, Ap = -2, Damage = 2, Abilities = new WeaponAbilities()
                },
                ["Master-crafted Power Weapon"] = new()
                {
                    Name = "Master-crafted Power Weapon", Type = WeaponType.Melee, Range = 0, Attacks = 4,
                    Skill = 2, Strength = 5, Ap = -2, Damage = 1, Abilities = new WeaponAbilities()
                }
            });
    }
}
