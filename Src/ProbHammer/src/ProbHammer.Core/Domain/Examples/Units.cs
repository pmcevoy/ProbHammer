using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Core.Domain.Examples;

public static class Units
{
    public static AttachedUnit CrusaderSquad_Helbrecht_Ancient()
    {
        return new AttachedUnit(
            bodyguard: CrusaderSquadUnitWithChainSwords(),
            attached:
            [
                new Unit(
                    datasheet: Datasheets.HighMarshalHelbrecht(),
                    enhancements: [],
                    modelLines:
                    [
                        new ModelLine(
                            statlineName: "High Marshal Helbrecht",
                            weapons:
                            [
                                "Ferocity", "➤ Sword of the High Marshals - sweep",
                                "➤ Sword of the High Marshals - strike"
                            ],
                            count: 1)
                    ]),
                new Unit(
                    datasheet: Datasheets.CrusadeAncient(),
                    enhancements: [],
                    modelLines:
                    [
                        new ModelLine(
                            statlineName: "Crusade Ancient",
                            weapons: ["Bold Pistol", "Master-crafted power weapon"],
                            count: 1)
                    ])
            ]
        );
    }

    public static AttachedUnit CrusaderSquad_Marshal_Lieutenant()
    {
        return new AttachedUnit(
            bodyguard: CrusaderSquadUnitWithChainSwords(),
            attached:
            [
                new Unit(
                    datasheet: Datasheets.Marshal(),
                    enhancements: [],
                    modelLines:
                    [
                        new ModelLine(
                            statlineName: "Marshal",
                            weapons: ["Combi-weapon", "Master-crafted power weapon", "Close combat weapon"],
                            count: 1)
                    ]),
                new Unit(
                    datasheet: Datasheets.Lieutenant(),
                    enhancements: [],
                    modelLines:
                    [
                        new ModelLine(
                            statlineName: "Lieutenant",
                            weapons: ["Bold Pistol", "Master-crafted power weapon", "Close combat weapon"],
                            count: 1)
                    ])
            ]
        );
    }

    public static AttachedUnit SwordBretheren_Marshal()
    {
        return new AttachedUnit(
            bodyguard: new Unit(
                datasheet: Datasheets.SwordBrethernSquad(),
                enhancements: [],
                modelLines:
                [
                    new ModelLine(
                        statlineName: "Sword Brother",
                        weapons:
                        [
                            "Heavy bolt pistol",
                            "Master-crafted power weapon"
                        ],
                        count: 4
                    )
                ]
            ),
            attached:
            [
                new Unit(
                    datasheet: Datasheets.Marshal(),
                    enhancements: [],
                    modelLines:
                    [
                        new ModelLine(
                            statlineName: "Marshal",
                            weapons: ["Combi-weapon", "Master-crafted power weapon", "Close combat weapon"],
                            count: 1)
                    ])
            ]
        );
    }

    public static Unit AssaultIntercessorSquad()
    {
        return new Unit(
            datasheet: Datasheets.AssaultIntercessorSquad(),
            enhancements: [],
            modelLines:
            [
                new ModelLine(
                    statlineName: "Assault Intercessor Sergeant",
                    weapons: ["Heavy Bolt pistol", "Astartes chainsword"],
                    count: 1),
                new ModelLine(
                    statlineName: "Assault Intercessor",
                    weapons: ["Heavy Bolt pistol", "Astartes chainsword"],
                    count: 4)
            ]);

    }

    public static Unit Impulsor()
    {
        return new Unit(
            datasheet: Datasheets.Impulsor(),
            enhancements: [],
            modelLines:
            [
                new ModelLine(
                    statlineName: "Impulsor",
                    weapons: [
                        "Storm Bolter",
                        "Armoured hull",
                        "Multi-melta",
                        "Heavy Bolt pistol"
                    ],
                    count: 1)
            ]);

    }

    public static Unit ScoutSquad()
    {
        return new Unit(
            datasheet: Datasheets.ScoutSquad(),
            enhancements: [],
            modelLines:
            [
                new ModelLine(
                    statlineName: "Scout Sergeant",
                    weapons: ["Bolt pistol", "Astartes chainsword", "Close combat weapon"],
                    count: 1),
                new ModelLine(
                    statlineName: "Scout",
                    weapons: ["Astartes shotgun", "Bolt pistol", "Close combat weapon"],
                    count: 4)
            ]);

    }
    
    private static Unit CrusaderSquadUnitWithChainSwords()
    {
        return new Unit(
            datasheet: Datasheets.CrusaderSquad(),
            enhancements: [],
            modelLines:
            [
                new ModelLine(
                    statlineName: "Neophyte",
                    weapons: ["Bolt pistol", "Astartes chainsword"],
                    4),
                new ModelLine(
                    statlineName: "Initiate",
                    weapons: ["Bolt pistol", "Heavy Bolt pistol", "Power fist"],
                    count: 2),
                new ModelLine(
                    statlineName: "Initiate",
                    weapons: ["Bolt pistol", "Heavy Bolt pistol", "Astartes chainsword"],
                    count: 3),
                new ModelLine(statlineName: "Sword Brother",
                    weapons: ["Master-crafted power weapon", "Pyre pistol"],
                    count: 1)
            ]);
    }
}