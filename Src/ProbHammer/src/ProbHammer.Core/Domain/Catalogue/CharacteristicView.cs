namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed per-kind subtypes (<see cref="ScalarCharacteristicView"/>,
/// <see cref="InvulnerableSaveCharacteristicView"/>) rather than a generic
/// CharacteristicView&lt;T&gt; - see introduce-characteristic-domain-model/design.md's Decision 2.
/// Captures, per characteristic property, the abilities touching it
/// (<see cref="ContributingAbilities"/>); each subtype adds its own typed OriginalValue/DerivedValue,
/// since those differ in shape per kind and so can't live on this shared base.</summary>
public abstract record CharacteristicView(IReadOnlyList<Ability> ContributingAbilities)
{
    /// <summary>True exactly when DerivedValue is null - overridden per subtype, since DerivedValue
    /// itself is declared there, not on this base.</summary>
    public abstract bool IsCaveated { get; }

    /// <summary>Compares <see cref="ContributingAbilities"/> by content, not by list reference - the
    /// compiler-generated record equality would otherwise compare this <c>IReadOnlyList&lt;Ability&gt;</c>
    /// field by reference (arrays/<c>List&lt;T&gt;</c> don't override <c>Equals</c>), so two views
    /// holding the same abilities in independently-constructed lists would never compare equal.</summary>
    public virtual bool Equals(CharacteristicView? other) =>
        other is not null
        && GetType() == other.GetType()
        && ContributingAbilities.SequenceEqual(other.ContributingAbilities);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var ability in ContributingAbilities) hash.Add(ability);
        return hash.ToHashCode();
    }

    /// <summary>Shared by every subtype's own <c>Value</c> property - the value a caller should
    /// actually use/display: <paramref name="derivedValue"/> once resolved, or
    /// <paramref name="originalValue"/> while still caveated.</summary>
    protected static T SelectValue<T>(bool isCaveated, T originalValue, T? derivedValue) where T : class =>
        isCaveated ? originalValue : derivedValue!;
}

public sealed record ScalarCharacteristicView(
    CharacteristicValue OriginalValue,
    CharacteristicValue? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    /// <summary>Redeclared to enforce the one real invariant: a caveated value (DerivedValue null)
    /// must carry at least one contributing ability to attribute the uncertainty to.</summary>
    public CharacteristicValue? DerivedValue { get; init; } = DerivedValue is null && ContributingAbilities.Count == 0
        ? throw new ArgumentException(
            "A caveated view must carry at least one contributing ability.", nameof(ContributingAbilities))
        : DerivedValue;

    public override bool IsCaveated => DerivedValue is null;

    /// <summary>The value a caller should actually use/display.</summary>
    public CharacteristicValue Value => SelectValue(IsCaveated, OriginalValue, DerivedValue);
}

public sealed record InvulnerableSaveCharacteristicView(
    InvulnerableSave OriginalValue,
    InvulnerableSave? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    /// <summary>Redeclared to enforce the one real invariant - see
    /// invulnerable-save's "Caveated Values Always Carry Their Source Ability": a caveated value
    /// (DerivedValue null) must carry at least one contributing ability.</summary>
    public InvulnerableSave? DerivedValue { get; init; } = DerivedValue is null && ContributingAbilities.Count == 0
        ? throw new ArgumentException(
            "A caveated view must carry at least one contributing ability.", nameof(ContributingAbilities))
        : DerivedValue;

    public override bool IsCaveated => DerivedValue is null;

    /// <summary>The value a caller should actually use/display.</summary>
    public InvulnerableSave Value => SelectValue(IsCaveated, OriginalValue, DerivedValue);

    /// <summary>No invulnerable save at all - mirrors DiceExpression.D3/D6's static-preset convention
    /// for this specific, frequently-constructed value.</summary>
    public static readonly InvulnerableSaveCharacteristicView None = Resolved(InvulnerableSave.None);

    /// <summary>Mirrors InvulnerableSave's own implicit int conversion - a uniform, non-caveated,
    /// no-contributing-abilities save is the common case at a construction call site.</summary>
    public static implicit operator InvulnerableSaveCharacteristicView(int uniformValue) => Resolved(uniformValue);

    /// <summary>A fully-known value with no contributing abilities - the common case (e.g. a plain,
    /// non-footnoted catalogue value). Named rather than a positional constructor overload so the
    /// call site states its own semantics, matching DiceExpression.Fixed's convention.</summary>
    public static InvulnerableSaveCharacteristicView Resolved(InvulnerableSave value) => Resolved(value, []);

    public static InvulnerableSaveCharacteristicView Resolved(int melee, int ranged) =>
        Resolved(new InvulnerableSave(melee, ranged));

    /// <summary>"Resolved" means not caveated (DerivedValue is set) - independent of whether any
    /// abilities are recorded as contributing to it. A matched, fully-understood StatlineFlagRule
    /// (e.g. Shield Dome) is exactly this case: resolved, but with its own matched ability recorded
    /// in ContributingAbilities.</summary>
    public static InvulnerableSaveCharacteristicView Resolved(
        InvulnerableSave value, IReadOnlyList<Ability> contributingAbilities) =>
        new(value, value, contributingAbilities);

    /// <summary>A value left caveated by exactly one unresolved contributing ability - see
    /// invulnerable-save's "Caveated Values Always Carry Their Source Ability".</summary>
    public static InvulnerableSaveCharacteristicView Caveated(InvulnerableSave value, Ability caveatAbility) =>
        new(value, null, [caveatAbility]);

    public static InvulnerableSaveCharacteristicView Caveated(int melee, int ranged, Ability caveatAbility) =>
        Caveated(new InvulnerableSave(melee, ranged), caveatAbility);
}
