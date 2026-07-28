using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Fixtures;

public class WeaponFixtures
{
    public static MeleeWeapon ChainSword = new(
        "Chainsword",
        A: 4, Ws: 3, S: 4, Ap: -1, D: 1
    );

    public static MeleeWeapon MasterCraftedPowerWeapon = new(
        "Master-crafted Power Weapon",
        A: 3, Ws: 2, S: 5, Ap: -2, D: 2
    ){LethalHits = true};

    public static MeleeWeapon PowerFist = new(
        "Power fist",
        A: 3, Ws: 3, S: 8, Ap: -2, D: 2
    );
    
    public static RangedWeapon BoltPistol = new(
        "Bolt Pistol",
        12,
        1, 3, 4, 0, 1
    ) { Pistol = true };
    
    public static RangedWeapon HeavyBoltPistol = new(
        "Heavy Bolt Pistol",
        18,
        1, 3, 4, -1, 1
    ) { Pistol = true };
}