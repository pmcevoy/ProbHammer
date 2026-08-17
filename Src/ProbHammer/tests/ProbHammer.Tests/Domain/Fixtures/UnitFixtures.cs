using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class UnitFixtures
{
    /// <summary>
    ///     4 identical sword brothers
    /// </summary>
    public static Unit SwordBrethrenSquadUniform()
    {
        var datasheet = DatasheetFixtures.SwordBrethrenSquad();

        return new Unit(
            datasheet,
            enhancements: [],
            modelLines:
            [
                new ModelLine("Sword Brother",
                    [WeaponFixtures.ChainSword().Name, WeaponFixtures.HeavyBoltPistol().Name], count: 4)
            ]);
    }

    /// <summary>5 Assault Intercessors, 4 troops, one sergeant</summary>
    public static Unit AssaultIntercessorSquadWithUnitLeader()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        return new Unit(
            datasheet,
            enhancements: [],
            modelLines:
            [
                new ModelLine("Assault Intercessor Sergeant",
                    [WeaponFixtures.ChainSword().Name, WeaponFixtures.HeavyBoltPistol().Name], count: 1),
                new ModelLine("Assault Intercessor",
                    [WeaponFixtures.ChainSword().Name, WeaponFixtures.HeavyBoltPistol().Name], count: 4)
            ]);
    }

    /// <summary>
    ///     Crusader Squad with a mixed loadout within the shared "Initiate" statline: 2x Power
    ///     Fist / 3x Master-crafted Power Weapon - matches the roster-model spec scenario verbatim.
    /// </summary>
    public static Unit CrusaderSquadMixedLoadout()
    {
        var datasheet = DatasheetFixtures.CrusaderSquad();

        return new Unit(
            datasheet,
            enhancements: [],
            modelLines:
            [
                new ModelLine("Initiate", [WeaponFixtures.PowerFist().Name], count: 2),
                new ModelLine("Initiate", [WeaponFixtures.MasterCraftedPowerWeapon().Name], count: 3)
            ]);
    }

    /// <summary>
    ///     Chaos Space Marine Squad's five distinct named model types, one model-line each, with
    ///     "Psyker" present only on the Gunner's own model-line - matches the "Masters of the
    ///     Maelstrom" card's "GARLON SOULEATER: PSYKER" per-named-individual keyword scoping,
    ///     proving it isn't per-statline-value (the Datasheet itself carries no PSYKER keyword).
    /// </summary>
    public static Unit ChaosSpaceMarineSquadWithPsyker()
    {
        var datasheet = DatasheetFixtures.ChaosSpaceMarineSquad();
        var weapons = new[] { WeaponFixtures.ChainSword().Name, WeaponFixtures.BoltPistol().Name };

        return new Unit(
            datasheet,
            enhancements: [],
            modelLines:
            [
                new ModelLine("Chaos Space Marine", weapons, count: 1),
                new ModelLine("Chaos Space Marine Champion", weapons, count: 1),
                new ModelLine("Icon Bearer", weapons, count: 1),
                new ModelLine("Chaos Space Marine Gunner", weapons, count: 1, keywords: ["Psyker"]),
                new ModelLine("Chaos Space Marine Reaper", weapons, count: 1)
            ]);
    }
}