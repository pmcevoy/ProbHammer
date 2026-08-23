using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Examples;

public static class Datasheets
{
    public static Datasheet HighMarshalHelbrecht()
    {
        return new Datasheet(
            name: "High Marshal Helbrecht",
            statlines:
            [
                ("High Marshal Helbrecht", new Statline(6, 4, 2, 6, 6, 3) { InSv = 4 })
            ],
            weaponProfiles:
            [
                new RangedWeapon("Ferocity",
                    24, 2, 2, 5, -1, 2)
                {
                    DevastatingWounds = true,
                    Anti = new Dictionary<string, int>
                    {
                        ["Infantry"] = 4
                    }
                },
                new MeleeWeapon("➤ Sword of the High Marshals - sweep",
                    12, 2, 6, -3, 1),
                new MeleeWeapon("➤ Sword of the High Marshals - strike",
                    6, 2, 8, -3, 3)
            ],
            abilities:
            [
                new Ability
                {
                    Name = "LEADER",
                    Text =
                        "This model can be attached to the following units:\\n\\n■ Assault Intercessor Squad\\n■ Intercessor Squad\\n■ Crusader Squad\\n■ Sword Brethren",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                TemplarVows(),
                new Ability
                {
                    Name = "Crusade of Wrath",
                    Text =
                        "While this model is leading a unit, add 1 to the Attacks and Strength characteristic of melee weapons equipped by models in that unit.",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "High Marshal",
                    Text =
                        "At the start of the Fight phase, select one enemy unit within Engagement Range of this model’s unit and roll one D6, adding 1 to the result for every five models in this model's unit: on a 2-3, that enemy unit suffers D3 mortal wounds; on a 4-5, that enemy unit suffers 3 mortal wounds; on a 6, that enemy unit suffers D3+3 mortal wounds.",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
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

    public static Datasheet CrusadeAncient()
    {
        return new Datasheet(
            name: "Crusade Ancient",
            statlines:
            [
                ("Crusade Ancient", new Statline(6, 4, 3, 4, 6, 1))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Bolt Pistol", 12, 1, 3, 4, 0, 1) { Pistol = true },
                new MeleeWeapon("Master-crafted power weapon", 5, 2, 5, -2, 2)
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "SUPPORT",
                    Text =
                        "This model can be attached to the following units:\n\n■ CRUSADER SQUAD\n■ SWORD BRETHREN SQUAD\n\nYou can attach this model to a unit it can lead even if one Captain or Chapter Master model has already been attached to it. If you do, and that Bodyguard unit is destroyed, the Leader units attached to it become separate units, with their original Starting Strengths.",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Vengeful Exhortation",
                    Text =
                        "Fight on death on a 4+",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Martial Honour",
                    Text =
                        "Destory a unit, add 5 to OC",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                }
            },
            keywords:
            [
                "INFANTRY", "CHARACTER", "GRENADES", "IMPERIUM", "TACITUS", "ANCIENT", "CRUSADE ANCIENT"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    public static Datasheet Lieutenant()
    {
        return new Datasheet(
            name: "Lieutenant",
            statlines:
            [
                ("Lieutenant", new Statline(6, 4, 3, 4, 6, 1))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Bolt Pistol", 12, 1, 2, 4, 0, 1) { Pistol = true },
                new MeleeWeapon("Master-crafted power weapon", 5, 2, 5, -2, 2),
                new MeleeWeapon("Close combat weapon", 5, 2, 4, 0, 1)
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "SUPPORT",
                    Text =
                        "This model can be attached to the following units:\n\n■ ASSAULT INTERCESSOR SQUAD\n■ BLADEGUARD VETERAN SQUAD\n■ COMPANY HEROES\n■ CRUSADER SQUAD\n■ DEATHWATCH VETERANS\n■ DECIMUS KILL TEAM\n■ FORTIS KILL TEAM\n■ HELLBLASTER SQUAD\n■ INFERNUS SQUAD\n■ INNER CIRCLE COMPANIONS\n■ INTERCESSOR SQUAD\n■ STERNGUARD VETERAN SQUAD\n■ SWORD BRETHREN SQUAD\n■ TACTICAL SQUAD\n\nYou can attach this model to a unit it can lead even if one Captain or Chapter Master model has already been attached to it. If you do, and that Bodyguard unit is destroyed, the Leader units attached to it become separate units, with their original Starting Strengths.",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Tactical Precision",
                    Text =
                        "While this model is leading a unit, weapons equipped by models in that unit have the [LETHAL HITS] ability.",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Target Priority",
                    Text =
                        "This model’s unit is eligible to shoot and declare a charge in a turn in which it Fell Back",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
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
            statlines:
            [
                ("Marshal", new Statline(6, 4, 3, 5, 6, 1) { InSv = 4 })
            ],
            weaponProfiles:
            [
                new RangedWeapon("Combi-weapon", 24, 1, 3, 4, 0, 1)
                {
                    DevastatingWounds = true, RapidFire = 1, Anti = new Dictionary<string, int> { ["INFANTRY"] = 4 }
                },
                new MeleeWeapon("Master-crafted power weapon", 7, 2, 5, -2, 2)
                    { LethalHits = true },
                new MeleeWeapon("Close combat weapon", 5, 2, 4, 0, 1)
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "LEADER",
                    Text =
                        "This model can be attached to the following units:\n\n■ Assault Intercessor Squad\n■ Infernus Squad\n■ Intercessor Squad\n■ Crusader Squad\n■ Sword Brethren\n■ Sternguard Veteran Squad",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                TemplarVows(),
                new Ability
                {
                    Name = "Inspirational Exemplar",
                    Text =
                        "While this model is leading a unit, each time a model in that unit makes a melee attack, an unmodified Hit roll of 5+ scores a Critical Hit.",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Pious Fervour",
                    Text =
                        "Each time this model's unit is selected to fight, until the end of the phase, add 1 to the Attacks characteristic of this model's Master-Crafted Power Weapon for each enemy unit within 6\" of this model (to a maximum of +3)",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
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
            statlines:
            [
                ("Assault Intercessor Sergeant", new Statline(6, 4, 3, 2, 6, 2)),
                ("Assault Intercessor", new Statline(6, 4, 3, 2, 6, 2))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Heavy bolt pistol", 18, 1, 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Astartes chainsword", 4, 3, 4, -1, 1)
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Targetted Intercession",
                    Text = "Melee reroll wound 1. If in range of objective, reroll wound",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
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
            statlines:
            [
                ("Sword Brother", new Statline(6, 4, 3, 2, 6, 2)),
                ("Initiate", new Statline(6, 4, 3, 2, 6, 2)),
                ("Neophyte", new Statline(6, 4, 4, 2, 6, 2))
            ],
            weaponProfiles:
            [
                new MeleeWeapon("Master-crafted power weapon", 3, 2, 5, -2, 2)
                    { LethalHits = true },
                new RangedWeapon("Pyre pistol", 12, DiceExpression.D6, 0, 4, 0, 1)
                    { Pistol = true, Torrent = true, IgnoresCover = true },

                new RangedWeapon("Bolt pistol", 12, 1, 3, 4, 0, 1) { Pistol = true },
                new RangedWeapon("Heavy Bolt pistol", 18, 1, 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Close combat weapon", 3, 3, 4, 0, 1),
                new MeleeWeapon("Astartes chainsword", 4, 3, 4, -1, 1) { SustainedHits = 1 },
                new MeleeWeapon("Power fist", 3, 3, 8, -2, 2)
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Righteous Zeal",
                    Text = "Surge move D6+2",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                }
            },
            keywords:
            [
                "INFANTRY", "BATTLELINE", "GRENADES", "IMPERIUM", "TACITUS", "CRUSADER SQUAD"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

    /// <summary>Fixture added by `structured-invulnerable-save` specifically to give `/LivePlay`
    /// a real example of a non-uniform/caveated invulnerable save to render, since none of the
    /// existing example army's units have one. Uses real, verified values (from the live BSData
    /// clone, investigated during that change's design): Canis Rex's real InSv text "5+*"
    /// resolves - via the same entry-scoped ability resolution `BsdataDatasheetMapper` uses - to a
    /// caveated save with the real linked ability's exact text.</summary>
    public static Datasheet CanisRex()
    {
        var invulnerableSaveAbility = new Ability
        {
            Name = "Invulnerable Save (5+*)",
            Text = "This model has a 5+ invulnerable save against ranged attacks.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.Intrinsic
        };

        return new Datasheet(
            name: "Canis Rex",
            statlines:
            [
                ("Canis Rex", new Statline(8, 11, 3, 26, 6, 10)
                {
                    InSv = new InvulnerableSave(
                        meleeInSv: 5,
                        rangedInSv: 5,
                        caveated: true,
                        caveatAbility: invulnerableSaveAbility)
                })
            ],
            weaponProfiles:
            [
                new RangedWeapon("Rapid-fire battle cannon", 72, DiceExpression.D6 + 3, 3, 9, -2, 3) { Blast = true },
                new MeleeWeapon("Reaper chainsword", A: 8, Ws: 3, S: 12, Ap: -3, D: 3)
            ],
            abilities: [invulnerableSaveAbility],
            keywords: ["VEHICLE", "TITANIC", "IMPERIUM", "QUESTORIS", "CANIS REX"],
            factionKeywords: ["IMPERIUM", "IMPERIAL KNIGHTS"]
        );
    }

    /// <summary>Fixture added by `redesign-invulnerable-save-display` to give `/LivePlay` a second,
    /// independently-sourced caveated invulnerable save alongside `CanisRex()` - real, verified
    /// values (from the live BSData clone, traced during that change's design). Howling Banshees'
    /// real InSv text `"4+* / 5+"` resolves, via the same entry-scoped ability resolution
    /// `BsdataDatasheetMapper` uses, to a caveated save keeping only the unfootnoted `5+` digit
    /// (duplicated into both `MeleeInSv`/`RangedInSv`), linked to the real base-rules ability
    /// (id `9fa6-3128-6c5b-55f6`, `Warhammer 40,000.json`) whose text describes the melee-improved
    /// condition the footnote actually means.</summary>
    public static Datasheet HowlingBanshee()
    {
        var invulnerableSaveAbility = new Ability
        {
            Name = "Invulnerable Save (4+*)",
            Text = "Models in this unit have a 4+ invulnerable save against melee attacks.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.Intrinsic
        };

        return new Datasheet(
            name: "Howling Banshees",
            statlines:
            [
                ("Howling Banshee", new Statline(8, 3, 4, 1, 6, 1)
                {
                    InSv = new InvulnerableSave(
                        meleeInSv: 5,
                        rangedInSv: 5,
                        caveated: true,
                        caveatAbility: invulnerableSaveAbility)
                })
            ],
            weaponProfiles:
            [
                new RangedWeapon("Shuriken Pistol", 12, 1, 2, 4, -1, 1) { Assault = true, Pistol = true },
                new MeleeWeapon("Aeldari power sword", A: 4, Ws: 2, S: 4, Ap: -2, D: 1)
            ],
            abilities: [invulnerableSaveAbility],
            keywords: ["INFANTRY", "AELDARI", "HOWLING BANSHEES"],
            factionKeywords: ["AELDARI", "ASURYANI"]
        );
    }

    /// <summary>Explicitly synthetic fixture added by `redesign-invulnerable-save-display` to
    /// exercise `/LivePlay`'s "differing, both known, non-caveated" invulnerable-save render case.
    /// Unlike every other fixture in this file, its `InSv` value is hand-constructed and does NOT
    /// come from any real BSData catalogue entry: tracing `BsdataDatasheetMapper
    /// .ResolveInvulnerableSave` during this change's design confirmed no real source text can
    /// currently produce two independently-known, non-zero, differing melee/ranged values - a
    /// footnoted `/`-split always collapses to one duplicated (caveated) digit, and a parenthetical
    /// restriction always zeroes the other side. This fixture exists purely so the render logic has
    /// something to draw against; its name and numbers do not represent any real datasheet.</summary>
    public static Datasheet TwinWardSentinel()
    {
        return new Datasheet(
            name: "Twin-Ward Sentinel",
            statlines:
            [
                ("Twin-Ward Sentinel", new Statline(6, 5, 3, 6, 7, 2)
                {
                    InSv = new InvulnerableSave(
                        meleeInSv: 4,
                        rangedInSv: 5,
                        caveated: false,
                        caveatAbility: null)
                })
            ],
            weaponProfiles:
            [
                new RangedWeapon("Ward cannon", 24, 2, 3, 6, -1, 2),
                new MeleeWeapon("Guardian spear", A: 4, Ws: 3, S: 6, Ap: -1, D: 2)
            ],
            abilities: [],
            keywords: ["VEHICLE"],
            factionKeywords: ["UNALIGNED FORCES"]
        );
    }

    public static Datasheet Impulsor()
    {
        return new Datasheet(
            name: "Impulsor",
            statlines:
            [
                ("Impulsor", new Statline(12, 9, 3, 11, 6, 2))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Storm Bolter", 24, 2, 3, 4, 0, 1) { RapidFire = 2 },
                new MeleeWeapon("Armoured hull", A: 3, Ws: 4, S: 6, Ap: 0, D: 1),
                new RangedWeapon("Multi-melta", 18, 2, 3, 9, -4, DiceExpression.D6) { Melta = 2 },
                new RangedWeapon("Heavy Bolt pistol", 18, 1, 3, 4, -1, 1) { Pistol = true }
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Deadly Demise D3",
                    Text = "roll, mortal wounds (TBC)",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Firing deck 6",
                    Text = "Models inside..",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Shield Dome",
                    Text = "The bearer as a 5+ invulnerable save",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Assault Vehicle",
                    Text = "Units can disembark after advance",
                    Scope = AbilityScope.Model,
                    Origin = AbilityOrigin.Intrinsic
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
            statlines:
            [
                ("Scout Sergeant", new Statline(6, 4, 4, 2, 6, 1)),
                ("Scout", new Statline(6, 4, 4, 2, 6, 1))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Bolt pistol", 12, 1, 3, 4, 0, 1) { Pistol = true },
                new MeleeWeapon("Close combat weapon", 2, 3, 4, 0, 1),
                new MeleeWeapon("Astartes chainsword", 4, 3, 4, -1, 1),
                new RangedWeapon("Astartes shotgun", 18, 2, 3, 4, 0, 1) { Assault = true }
            ],
            abilities: new[]
            {
                new Ability
                {
                    Name = "Infiltrators",
                    Text = "deploy center table",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },
                new Ability
                {
                    Name = "Scouts 6",
                    Text = "6in move at start of game",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                },

                new Ability
                {
                    Name = "Guerrilla tactics",
                    Text = "Back to reserves",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
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
            statlines:
            [
                ("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))
            ],
            weaponProfiles:
            [
                new RangedWeapon("Heavy bolt pistol", 18, 1, 3, 4, -1, 1) { Pistol = true },
                new MeleeWeapon("Master-crafted power weapon", 3, 2, 5, -2, 2) { LethalHits = true }
            ],
            abilities: new[]
            {
                TemplarVows(),
                new Ability
                {
                    Name = "Exploit their cowardice",
                    Text = "Normal move if enemy falls back",
                    Scope = AbilityScope.Unit,
                    Origin = AbilityOrigin.Intrinsic
                }
            },
            keywords:
            [
                "INFANTRY", "GRENADES", "IMPERIUM", "TACITUS", "SWORD BRETHEREN SQUARD"
            ],
            factionKeywords: ["APEPTUS ASTARTES", "BLACK TEMPLARS"]
        );
    }

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
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.Intrinsic
        };
    }
}