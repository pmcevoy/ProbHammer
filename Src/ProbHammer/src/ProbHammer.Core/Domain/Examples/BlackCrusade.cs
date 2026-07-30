using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Simulation;

namespace ProbHammer.Core.Domain.Examples;

public static class BlackCrusade
{
    private static Ability TemplarVows()
    {
        return new Ability
        {
            Name = "TEMPLAR VOWS",
            Text =
                "If your Army Faction is **^^Adeptus Astartes^^**, at the start of the first battle round, select one of the following Vows to be active for **^^Adeptus Astartes^^** units from your army. " +
                "While a Vow is active for your army, that unit has the associated ability below.",
            Choices =
            [
                new AbilityChoice("Abhor the Witch, Destroy the Witch",
                    "Each time this unit declares a charge, if one or more targets of that charge have the **^^Psyker^^** keyword, you can re-roll the Charge roll. Melee weapons equipped by models in this unit have the **[PRECISION]** ability while targeting **^^Psyker^^** units."),
                new AbilityChoice("Accept Any Challenge, No Matter the Odds",
                    "Each time a model in this unit makes a melee attack, if the Strength characteristic of that attack is less than or equal to the Toughness characteristic of the target, add 1 to the wound roll"),
                new AbilityChoice("Suffer Not the Unclean to Live",
                    "This unit is eligible to declare a charge in a turn in which it Fell Back, and each time a model in this unit makes a Pile-in or Consolidation move, it does not need to end that move closer to the closest enemy model, provided it ends that move as close as possible to the nearest enemy unit."),
                new AbilityChoice("Uphold the Honour of the Emperor",
                    "If this unit has the **^^Infantry^^** keyword:\\n■ At the end of your Command phase, if this unit is within range of an objective marker you control, that objective marker remains under your control until your opponent's level of control over that objective marker is greater than yours at the end of the phase. \\n■ If the mission you are playing features Actions, this unit is eligible to start to perform an Action in a turn in which it Advanced.")
            ],
            Scope = AbilityScope.Unit
        };
    }

    public static Datasheet HighMarshalHelbrecht()
    {
        return new Datasheet(
            name: "High Marshal Helbrecht",
            statlines: new Dictionary<string, Statline>
            {
                ["High Marshal Helbrecht"] = new(6, 4, 2, 6, 6, 3) { InSv = 4 }
            },
            weaponProfiles:
            [
                new RangedWeapon("Ferocity",
                    24, DiceExpression.Fixed(2), 2, 5, -1, 2)
                {
                    DevastatingWounds = true,
                    Anti = new Dictionary<string, int>
                    {
                        ["Infantry"] = 4
                    }
                },
                new MeleeWeapon("➤ Sword of the High Marshals - sweep",
                    DiceExpression.Fixed(12), 2, 6, -3, 1),
                new MeleeWeapon("➤ Sword of the High Marshals - strike",
                    DiceExpression.Fixed(6), 2, 8, -3, 3)
            ],
            abilities:
            [
                new Ability
                {
                    Name = "LEADER",
                    Text =
                        "This model can be attached to the following units:\\n\\n■ Assault Intercessor Squad\\n■ Intercessor Squad\\n■ Crusader Squad\\n■ Sword Brethren",
                    Scope = AbilityScope.Model
                },
                TemplarVows(),
                new Ability
                {
                    Name = "Crusade of Wrath",
                    Text =
                        "While this model is leading a unit, add 1 to the Attacks and Strength characteristic of melee weapons equipped by models in that unit.",
                    Scope = AbilityScope.Unit
                },
                new Ability
                {
                    Name = "High Marshal",
                    Text =
                        "At the start of the Fight phase, select one enemy unit within Engagement Range of this model’s unit and roll one D6, adding 1 to the result for every five models in this model's unit: on a 2-3, that enemy unit suffers D3 mortal wounds; on a 4-5, that enemy unit suffers 3 mortal wounds; on a 6, that enemy unit suffers D3+3 mortal wounds.",
                    Scope = AbilityScope.Model
                }
            ],
            keywords:
            [
                "INFANTRY", "CHARACTER", "EPIC HERO", "GRENADES", "IMPERIUM", "TACITUS", "CHAPTER MASTER",
                "HIGH MARSHAL HELBRECHT"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    public static Datasheet Lieutenant()
    {
        return new Datasheet(
            name: "Lieutenant",
            statlines: new Dictionary<string, Statline>
            {
                ["Lieutenant"] = new(6, 4, 3, 4, 6, 1)
            },
            weaponProfiles:
            [
                new RangedWeapon("Bold Pistol", 12, DiceExpression.Fixed(1), 2, 4, 0, 1) { Pistol = true },
                new MeleeWeapon("Master-crafted power weapon", DiceExpression.Fixed(5), 2, 5, -2, 2),
                new MeleeWeapon("Close combat weapon", DiceExpression.Fixed(5), 2, 4, 0, 1)
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "SUPPORT",
                    Text =
                        "This model can be attached to the following units:\n\n■ ASSAULT INTERCESSOR SQUAD\n■ BLADEGUARD VETERAN SQUAD\n■ COMPANY HEROES\n■ CRUSADER SQUAD\n■ DEATHWATCH VETERANS\n■ DECIMUS KILL TEAM\n■ FORTIS KILL TEAM\n■ HELLBLASTER SQUAD\n■ INFERNUS SQUAD\n■ INNER CIRCLE COMPANIONS\n■ INTERCESSOR SQUAD\n■ STERNGUARD VETERAN SQUAD\n■ SWORD BRETHREN SQUAD\n■ TACTICAL SQUAD\n\nYou can attach this model to a unit it can lead even if one Captain or Chapter Master model has already been attached to it. If you do, and that Bodyguard unit is destroyed, the Leader units attached to it become separate units, with their original Starting Strengths.",
                    Scope = AbilityScope.Model
                },
                new Ability
                {
                    Name = "Tactical Precision",
                    Text =
                        "While this model is leading a unit, weapons equipped by models in that unit have the [LETHAL HITS] ability.",
                    Scope = AbilityScope.Unit
                },
                new Ability
                {
                    Name = "Target Priority",
                    Text =
                        "This model’s unit is eligible to shoot and declare a charge in a turn in which it Fell Back",
                    Scope = AbilityScope.Unit
                }
            },
            keywords:
            [
                "INFANTRY", "CHARACTER", "GRENADES", "IMPERIUM", "TACITUS", "LIEUTENANT"
            ],
            factionKeywords: ["APEPTUS ASTARTES"]
        );
    }

    public static Datasheet Marshal()
    {
        return new Datasheet(
            name: "Marshal",
            statlines: new Dictionary<string, Statline>
            {
                ["Marshal"] = new(6, 4, 3, 5, 6, 1) { InSv = 4 }
            },
            weaponProfiles:
            [
                new RangedWeapon("Combi-weapon", 24, DiceExpression.Fixed(1), 3, 4, 0, 1)
                {
                    DevastatingWounds = true, RapidFire = 1, Anti = new Dictionary<string, int> { ["INFANTRY"] = 4 }
                },
                new MeleeWeapon("Master-crafted power weapon", DiceExpression.Fixed(7), 2, 5, -2, 2)
                    { LethalHits = true },
                new MeleeWeapon("Close combat weapon", DiceExpression.Fixed(5), 2, 4, 0, 1)
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "LEADER",
                    Text =
                        "This model can be attached to the following units:\n\n■ Assault Intercessor Squad\n■ Infernus Squad\n■ Intercessor Squad\n■ Crusader Squad\n■ Sword Brethren\n■ Sternguard Veteran Squad",
                    Scope = AbilityScope.Model
                },
                TemplarVows(),
                new Ability
                {
                    Name = "Inspirational Exemplar",
                    Text =
                        "While this model is leading a unit, each time a model in that unit makes a melee attack, an unmodified Hit roll of 5+ scores a Critical Hit.",
                    Scope = AbilityScope.Unit
                },
                new Ability
                {
                    Name = "Pious Fervour",
                    Text =
                        "Each time this model's unit is selected to fight, until the end of the phase, add 1 to the Attacks characteristic of this model's Master-Crafted Power Weapon for each enemy unit within 6\" of this model (to a maximum of +3)",
                    Scope = AbilityScope.Model
                }
            },
            keywords:
            [
                "INFANTRY", "CHARACTER", "GRENADES", "IMPERIUM", "TACITUS", "CAPTAIN", "MARSHAL"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    public static Datasheet AssaultIntercessorSquad()
    {
        return new Datasheet(
            name: "Assault Intercessor Squad",
            statlines: new Dictionary<string, Statline>
            {
                ["Assault Intercessor Sergeant"] = new(6, 4, 3, 2, 6, 2),
                ["Assault Intercessor"] = new(6, 4, 3, 2, 6, 2)
            },
            weaponProfiles:
            [
                new RangedWeapon("Heavy bolt pistol", 18, DiceExpression.Fixed(1), 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Astartes chainsword", DiceExpression.Fixed(4), 3, 4, -1, 1)
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Targetted Intercession",
                    Text = "Melee reroll wound 1. If in range of objective, reroll wound",
                    Scope = AbilityScope.Unit
                }
            },
            keywords:
            [
                "INFANTRY", "BATTLELINE", "GRENADES", "IMPERIUM", "TACITUS", "ASSAULT INTERCESSOR SQUARD"
            ],
            factionKeywords: ["APEPTUS ASTARTES"]
        );
    }

    public static Datasheet CrusaderSquad()
    {
        return new Datasheet(
            name: "Crusader Squad",
            statlines: new Dictionary<string, Statline>
            {
                ["Sword Brother"] = new(6, 4, 3, 2, 6, 2),
                ["Initiate"] = new(6, 4, 3, 2, 6, 2),
                ["Neophyte"] = new(6, 4, 4, 2, 6, 2)
            },
            weaponProfiles:
            [
                new MeleeWeapon("Master-crafted power weapon", DiceExpression.Fixed(3), 2, 5, -2, 2)
                    { LethalHits = true },
                new RangedWeapon("Pyre pistol", 12, new DiceExpression { Sides = 6, Count = 1 }, 0, 4, 0, 1)
                    { Pistol = true, Torrent = true, IgnoresCover = true },

                new RangedWeapon("Bolt pistol", 12, DiceExpression.Fixed(1), 3, 4, 0, 1) { Pistol = true },
                new RangedWeapon("Heavy Bolt pistol", 18, DiceExpression.Fixed(1), 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Close combat weapon", DiceExpression.Fixed(3), 3, 4, 0, 1),
                new MeleeWeapon("Astartes chainsword", DiceExpression.Fixed(4), 3, 4, -1, 1) { SustainedHits = 1 },
                new MeleeWeapon("Power fist", DiceExpression.Fixed(3), 3, 8, -2, 2)
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Righteous Zeal",
                    Text = "Surge move D6+2",
                    Scope = AbilityScope.Unit
                }
            },
            keywords:
            [
                "INFANTRY", "BATTLELINE", "GRENADES", "IMPERIUM", "TACITUS", "CRUSADER SQUAD"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    public static Datasheet Impulsor()
    {
        return new Datasheet(
            name: "Impulsor",
            statlines: new Dictionary<string, Statline>
            {
                ["Impulsor"] = new(12, 9, 3, 11, 6, 2)
            },
            weaponProfiles:
            [
                new RangedWeapon("Storm Bolter", 24, DiceExpression.Fixed(2), 3, 4, 0, 1) { RapidFire = 2 },
                new MeleeWeapon("Armoured hull", DiceExpression.Fixed(3), 4, 6, 0, 1),
                new RangedWeapon("Multi-melta", 18, DiceExpression.Fixed(2), 3, 9, -4, 6)
                    { Melta = 2 }, //NOTE D needs to be "D6"
                new RangedWeapon("Heavy Bolt pistol", 18, DiceExpression.Fixed(1), 3, 4, -1, 1) { Pistol = true }
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Deadly Demise D3",
                    Text = "roll, mortal wounds (TBC)",
                    Scope = AbilityScope.Model
                },
                new Ability
                {
                    Name = "Firing deck 6",
                    Text = "Models inside..",
                    Scope = AbilityScope.Model
                },
                new Ability
                {
                    Name = "Shield Dome",
                    Text = "The bearer as a 5+ invulnerable save",
                    Scope = AbilityScope.Model
                },
                new Ability
                {
                    Name = "Assault Vehicle",
                    Text = "Units can disembark after advance",
                    Scope = AbilityScope.Model
                }
            },
            keywords:
            [
                "VEHICLE", "TRANSPORT", "DEDICATED TRANSPORT", "IMPERIUM", "FRAME", "IMPULSOR"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    public static Datasheet ScoutSquad()
    {
        return new Datasheet(
            name: "Scout Squad",
            statlines: new Dictionary<string, Statline>
            {
                ["Scout Sergeant"] = new(6, 4, 4, 2, 6, 1),
                ["Scout"] = new(6, 4, 4, 2, 6, 1)
            },
            weaponProfiles:
            [
                new RangedWeapon("Bolt pistol", 12, DiceExpression.Fixed(1), 3, 4, 0, 1) { Pistol = true },
                new MeleeWeapon("Close combat weapon", DiceExpression.Fixed(2), 3, 4, 0, 1),
                new MeleeWeapon("Astartes chainsword", DiceExpression.Fixed(4), 3, 4, -1, 1),
                new RangedWeapon("Astartes shotgun", 18, DiceExpression.Fixed(2), 3, 4, 0, 1) { Assault = true }
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "Infiltrators",
                    Text = "deploy center table",
                    Scope = AbilityScope.Unit
                },
                new Ability
                {
                    Name = "Scouts 6",
                    Text = "6in move at start of game",
                    Scope = AbilityScope.Unit
                },

                new Ability
                {
                    Name = "Guerrilla tactics",
                    Text = "Back to reserves",
                    Scope = AbilityScope.Unit
                }
            },
            keywords:
            [
                "INFANTRY", "GRENADES", "SMOKE", "IMPERIUM", "SCOUT SQUARD"
            ],
            factionKeywords: ["APEPTUS ASTARTES"]
        );
    }
    
    public static Datasheet SwordBrethernSquad()
    {
        return new Datasheet(
            name: "Sword Bretheren Squad",
            statlines: new Dictionary<string, Statline>
            {
                ["Sword Brother"] = new(6, 4, 3, 3, 6, 1),
            },
            weaponProfiles:
            [
                new RangedWeapon("Heavy bolt pistol", 18, DiceExpression.Fixed(1), 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Master-crafted power weapon", DiceExpression.Fixed(3), 2, 5, -2, 2) {LethalHits = true},
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Exploit their cowardice",
                    Text = "Normal move if enemy falls back",
                    Scope = AbilityScope.Unit
                }
            },
            keywords:
            [
                "INFANTRY", "GRENADES", "IMPERIUM", "TACITUS", "SWORD BRETHEREN SQUARD"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }
}