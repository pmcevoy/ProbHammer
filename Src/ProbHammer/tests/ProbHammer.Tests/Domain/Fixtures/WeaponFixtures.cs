using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Simulation;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class WeaponFixtures
{
    public static MeleeWeapon ChainSword()
    {
        return new MeleeWeapon(
            "Chainsword",
            DiceExpression.Fixed(4), 3, 4, -1, 1
        );
    }

    public static MeleeWeapon MasterCraftedPowerWeapon()
    {
        return new MeleeWeapon(
            "Master-crafted Power Weapon",
            DiceExpression.Fixed(3), 2, 5, -2, 2
        ) { LethalHits = true };
    }

    public static MeleeWeapon PowerFist()
    {
        return new MeleeWeapon(
            "Power fist",
            DiceExpression.Fixed(3), 3, 8, -2, 2
        );
    }

    public static RangedWeapon BoltPistol()
    {
        return new RangedWeapon(
            "Bolt Pistol",
            12,
            DiceExpression.Fixed(1), 3, 4, 0, 1
        ) { Pistol = true };
    }

    public static RangedWeapon HeavyBoltPistol()
    {
        return new RangedWeapon(
            "Heavy Bolt Pistol",
            18,
            DiceExpression.Fixed(1), 3, 4, -1, 1
        ) { Pistol = true };
    }
}
