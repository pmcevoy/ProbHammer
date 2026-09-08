namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>The three arithmetic families a characteristic's own rulebook Improve/Worsen wording
/// resolves through - see introduce-characteristic-modification-kind/design.md's "Closed
/// Arithmetic-Family Classification" decision. A plain enum, not a <see cref="CharacteristicValue"/>
/// -style abstract/sealed hierarchy: a Kind selects behavior (which sign rule applies), with the same
/// int-in/int-out shape in every case, not a value that varies in shape per case.</summary>
public enum CharacteristicModificationKind
{
    /// <summary>WS, BS, Sv, Ld - an "N+" die-roll bar, where a numerically lower value is better, so
    /// Improve subtracts and Worsen adds.</summary>
    RollThreshold,

    /// <summary>AP - stored as a negative integer (see root CLAUDE.md's "AP is stored as a negative
    /// integer" convention), where a numerically lower value is a stronger penetration, so Improve
    /// subtracts (moving further from zero) and Worsen adds (moving toward zero, capped at 0 by
    /// <see cref="CharacteristicModificationClamp"/>).</summary>
    ArmourPenetration,

    /// <summary>Every other in-scope characteristic (M, T, W, Oc, S, and the length-valued Range
    /// characteristic) - a numerically higher value is better, so the stated amount applies with no
    /// inversion.</summary>
    Plain
}

/// <summary>Closed characteristic-name -> <see cref="CharacteristicModificationKind"/> lookup,
/// keyed by the same plain characteristic-name strings <see cref="CharacteristicEffect.Characteristic"/>
/// already uses. Deliberately excludes InSv and WeaponProfile's Attacks/Damage - see
/// introduce-characteristic-modification-kind/design.md's Context/Non-Goals: InSv is a compound
/// melee/ranged <see cref="InvulnerableSave"/>, not a plain scalar, and Attacks/Damage are
/// <see cref="DiceExpression"/>, never a plain scalar, in this codebase today.</summary>
public static class CharacteristicModificationKinds
{
    private static readonly Dictionary<string, CharacteristicModificationKind> Kinds =
        new()
        {
            ["WS"] = CharacteristicModificationKind.RollThreshold,
            ["BS"] = CharacteristicModificationKind.RollThreshold,
            ["Sv"] = CharacteristicModificationKind.RollThreshold,
            ["Ld"] = CharacteristicModificationKind.RollThreshold,
            ["AP"] = CharacteristicModificationKind.ArmourPenetration,
            ["M"] = CharacteristicModificationKind.Plain,
            ["T"] = CharacteristicModificationKind.Plain,
            ["W"] = CharacteristicModificationKind.Plain,
            ["Oc"] = CharacteristicModificationKind.Plain,
            ["S"] = CharacteristicModificationKind.Plain,
            ["Range"] = CharacteristicModificationKind.Plain
        };

    /// <summary>Throws for a characteristic name this component doesn't cover (including InSv and
    /// Attacks/Damage) rather than guessing - see this type's own doc comment for why those are
    /// excluded.</summary>
    public static CharacteristicModificationKind Of(string characteristic) =>
        Kinds.TryGetValue(characteristic, out var kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic,
                "Characteristic is not covered by CharacteristicModificationKind - see its own doc comment for exclusions.");
}
