using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class AggregateViewFixtures
{
    /// <summary>4 Bodyguard models carrying a weapon profile with 3 Attacks each, and an attached
    /// Leader carrying a differently-named weapon with an identical structural profile but 7
    /// Attacks - proves aggregation groups by profile equality (not weapon name) and computes a
    /// true summed total (4x3 + 7 = 19), not a count-only merge that discards one contributor's A.
    /// Mirrors the real fixture scenario in Examples/Units.cs's SwordBretheren_Marshal().</summary>
    public static AttachedUnit WeaponAggregationAttachedUnit()
    {
        var bodyguardWeapon = new MeleeWeapon("Master-crafted power weapon", 3, 2, 5, -2, 2) { LethalHits = true };
        var bodyguardDatasheet = new Datasheet(
            name: "Sword Brethren Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY"],
            abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [bodyguardWeapon]);
        var bodyguard = new Unit(
            bodyguardDatasheet, [],
            [new ModelLine("Sword Brother", [bodyguardWeapon.Name], count: 4)]);

        var leaderWeapon = new MeleeWeapon("Master-crafted power weapon", 7, 2, 5, -2, 2) { LethalHits = true };
        var leaderDatasheet = new Datasheet(
            name: "Marshal",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY"],
            abilities: [],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))],
            weaponProfiles: [leaderWeapon]);
        var leader = new Unit(
            leaderDatasheet, [],
            [new ModelLine("Marshal", [leaderWeapon.Name], count: 1)]);

        return new AttachedUnit(bodyguard, [leader]);
    }

    /// <summary>Bodyguard and attached Leader each carry a same-named weapon whose structural
    /// profile differs only by LethalHits - proves the two copies are NOT combined, each keeping
    /// its own total Attacks.</summary>
    public static AttachedUnit DifferentlyModifiedWeaponsAttachedUnit()
    {
        var bodyguardWeapon = new MeleeWeapon("Master-crafted power weapon", 3, 2, 5, -2, 2) { LethalHits = true };
        var bodyguardDatasheet = new Datasheet(
            name: "Sword Brethren Squad",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["INFANTRY"],
            abilities: [],
            statlines: [("Sword Brother", new Statline(6, 4, 3, 3, 6, 1))],
            weaponProfiles: [bodyguardWeapon]);
        var bodyguard = new Unit(
            bodyguardDatasheet, [],
            [new ModelLine("Sword Brother", [bodyguardWeapon.Name], count: 4)]);

        var leaderWeapon = new MeleeWeapon("Master-crafted power weapon", 7, 2, 5, -2, 2); // no LethalHits
        var leaderDatasheet = new Datasheet(
            name: "Marshal",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY"],
            abilities: [],
            statlines: [("Marshal", new Statline(6, 4, 3, 5, 6, 1))],
            weaponProfiles: [leaderWeapon]);
        var leader = new Unit(
            leaderDatasheet, [],
            [new ModelLine("Marshal", [leaderWeapon.Name], count: 1)]);

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
            abilities: [new Ability { Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic }],
            statlines: [("Initiate", new Statline(6, 4, 3, 2, 6, 2))],
            weaponProfiles: []);
        var bodyguard = new Unit(bodyguardDatasheet, [], [new ModelLine("Initiate", [], count: 5)]);

        var leaderDatasheet = new Datasheet(
            name: "Chaplain",
            factionKeywords: ["ADEPTUS ASTARTES"],
            keywords: ["CHARACTER", "INFANTRY"],
            abilities: [],
            statlines: [("Chaplain", new Statline(6, 4, 3, 4, 6, 1))],
            weaponProfiles: []);

        var ironHalo = new Ability { Name = "Iron Halo", Text = "This model has a 4+ invulnerable save.", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic };
        var leaderLine = new ModelLine("Chaplain", [], count: 1, abilities: [ironHalo]);
        var leader = new Unit(leaderDatasheet, [ironHalo], [leaderLine]);

        return new AttachedUnit(bodyguard, [leader]);
    }
}
