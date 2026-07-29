using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class UnitFixtures
{
    /// <summary>5 Assault Intercessors, all equipped identically (one model-line).</summary>
    public static Unit AssaultIntercessorSquadUniform()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        return new Unit(
            datasheet,
            enhancements: [],
            modelLines: [new ModelLine("Assault Intercessor", [WeaponFixtures.ChainSword().Name, WeaponFixtures.HeavyBoltPistol().Name], count: 5)]);
    }

    /// <summary>Crusader Squad with a mixed loadout within the shared "Initiate" statline: 2x Power
    /// Fist / 3x Master-crafted Power Weapon - matches the roster-model spec scenario verbatim.</summary>
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
}
