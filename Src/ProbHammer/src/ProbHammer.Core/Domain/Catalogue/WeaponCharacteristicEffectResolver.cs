namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves a classified <see cref="WeaponCharacteristicEffect"/> plus its source
/// <see cref="Ability"/> into a mutated <see cref="WeaponProfile"/> - the weapon-specific
/// counterpart to <see cref="CharacteristicModificationResolver"/>'s own scalar resolution and
/// <see cref="InvulnerableSaveEffectResolver"/>'s own compound-value resolution.
/// <c>ProbHammer.Core.Domain.Roster.AttachedUnitAggregator</c> is this resolver's real runtime
/// consumer, matched against each present ability's normalized Text in the checked-in
/// <see cref="RuleClassificationBaseline"/>.</summary>
public static class WeaponCharacteristicEffectResolver
{
    /// <summary>Mutates exactly the field <paramref name="effect"/>'s own Characteristic names
    /// ("S"/"AP"/"D" only - throws for any other value, including "A"; see this method's own thrown
    /// exception for why), preserving every other field of <paramref name="current"/> unchanged. The
    /// mutated field's pre-mutation original value is preserved from its own existing OriginalValue -
    /// never its effective Value, which may already reflect an earlier mutation - and its
    /// contributing abilities record <paramref name="sourceAbility"/>.</summary>
    public static WeaponProfile Resolve(
        WeaponCharacteristicEffect effect, Ability sourceAbility, WeaponProfile current)
    {
        var field = GetField(current, effect.Characteristic);
        var resolvedValue = CharacteristicModificationResolver.Resolve(
            effect.Characteristic, field.Value, effect.Verb, effect.Amount);
        var resolved = ScalarCharacteristicView.Resolved(field.OriginalValue, resolvedValue, [sourceAbility]);
        return SetField(current, effect.Characteristic, resolved);
    }

    private static ScalarCharacteristicView GetField(WeaponProfile profile, string characteristic) =>
        characteristic switch
        {
            "S" => profile.S,
            "AP" => profile.Ap,
            "D" => profile.D,
            _ => throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic,
                "WeaponCharacteristicEffectResolver only resolves S/AP/D - Attacks (\"A\") has no " +
                "ScalarCharacteristicView field to mutate (WeaponProfile.A stays a bare " +
                "DiceExpression - see CharacteristicModificationKind's own doc comment). Callers " +
                "must filter an Attacks-characteristic effect out before calling Resolve.")
        };

    private static WeaponProfile SetField(WeaponProfile profile, string characteristic, ScalarCharacteristicView value) =>
        characteristic switch
        {
            "S" => profile with { S = value },
            "AP" => profile with { Ap = value },
            "D" => profile with { D = value },
            _ => throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic, null)
        };
}
