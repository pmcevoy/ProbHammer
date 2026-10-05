namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves a classified <see cref="WeaponCharacteristicEffect"/> plus its source
/// <see cref="Ability"/> into a mutated <see cref="WeaponProfile"/> - the weapon-specific
/// counterpart to <see cref="CharacteristicModificationResolver"/>'s own scalar resolution and
/// <see cref="InvulnerableSaveEffectResolver"/>'s own compound-value resolution.
/// <c>ProbHammer.Core.Domain.Roster.AttachedUnitAggregator</c> is this resolver's real runtime
/// consumer, matched against each present ability.s classification in the
/// <see cref="AbilityClassificationCatalogue"/>.</summary>
public static class WeaponCharacteristicEffectResolver
{
    /// <summary>Mutates exactly the field <paramref name="effect"/>'s own Characteristic names
    /// ("S"/"AP"/"D", or "BS"/"WS" for the weapon's Skill - throws for any other value, including
    /// "A"; see this method's own thrown exception for why), preserving every other field of
    /// <paramref name="current"/> unchanged. The mutated field's pre-mutation original value is
    /// preserved from its own existing OriginalValue - never its effective Value, which may already
    /// reflect an earlier mutation - and its contributing abilities record
    /// <paramref name="sourceAbility"/>.</summary>
    public static WeaponProfile Resolve(
        WeaponCharacteristicEffect effect, Ability sourceAbility, WeaponProfile current)
    {
        var field = GetField(current, effect.Characteristic);
        var resolvedValue = CharacteristicModificationResolver.Resolve(
            effect.Characteristic, field.Value, effect.Verb, effect.Amount);
        var resolved = ScalarCharacteristicView.Resolved(field.OriginalValue, resolvedValue, [sourceAbility]);
        return SetField(current, effect.Characteristic, resolved);
    }

    /// <summary>A weapon has one Skill: "BS" names it on a ranged weapon, "WS" on a melee one. A Skill
    /// of 0 is BSData's "N/A" (e.g. a Torrent weapon), which a skill effect must not invent.</summary>
    public static bool SkillEffectApplies(string characteristic, WeaponProfile profile) =>
        characteristic switch
        {
            "BS" => profile is RangedWeapon && HasSkill(profile),
            "WS" => profile is MeleeWeapon && HasSkill(profile),
            _ => true
        };

    private static bool HasSkill(WeaponProfile profile) =>
        profile.Skill.Value is not NumericCharacteristicValue { Value: 0 };

    private static ScalarCharacteristicView GetField(WeaponProfile profile, string characteristic) =>
        (characteristic, profile) switch
        {
            ("S", _) => profile.S,
            ("AP", _) => profile.Ap,
            ("D", _) => profile.D,
            ("BS", RangedWeapon ranged) => ranged.Bs,
            ("WS", MeleeWeapon melee) => melee.Ws,
            ("BS" or "WS", _) => throw new ArgumentException(
                $"{characteristic} does not apply to a {profile.Type} weapon - callers must match the " +
                "skill to the weapon type first (SkillEffectApplies).", nameof(characteristic)),
            _ => throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic,
                "WeaponCharacteristicEffectResolver only resolves S/AP/D/BS/WS - Attacks (\"A\") has no " +
                "ScalarCharacteristicView field to mutate (WeaponProfile.A stays a bare " +
                "DiceExpression - see CharacteristicModificationKind's own doc comment). Callers " +
                "must filter an Attacks-characteristic effect out before calling Resolve.")
        };

    private static WeaponProfile SetField(WeaponProfile profile, string characteristic, ScalarCharacteristicView value) =>
        (characteristic, profile) switch
        {
            ("S", _) => profile with { S = value },
            ("AP", _) => profile with { Ap = value },
            ("D", _) => profile with { D = value },
            ("BS", RangedWeapon ranged) => ranged with { Bs = value },
            ("WS", MeleeWeapon melee) => melee with { Ws = value },
            _ => throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic, null)
        };
}
