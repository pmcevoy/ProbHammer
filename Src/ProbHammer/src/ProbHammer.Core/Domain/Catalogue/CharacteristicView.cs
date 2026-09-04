namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed per-kind subtypes (<see cref="ScalarCharacteristicView"/>,
/// <see cref="InvulnerableSaveCharacteristicView"/>) rather than a generic
/// CharacteristicView&lt;T&gt; - see introduce-characteristic-domain-model/design.md's Decision 2
/// for why a generic was considered and rejected. Captures, per characteristic property, the
/// abilities classified as touching it (<see cref="ContributingAbilities"/>); each subtype adds its
/// own typed OriginalValue/DerivedValue, since those genuinely differ in shape per kind (a bare
/// CharacteristicValue vs. an InvulnerableSave's melee/ranged pair) and so can't live on this
/// shared base.</summary>
public abstract record CharacteristicView(IReadOnlyList<Ability> ContributingAbilities)
{
    /// <summary>Computed per subtype from its own DerivedValue's nullability, never a constructor
    /// field - true exactly when DerivedValue is null, so the two can never disagree. (An earlier
    /// draft stored this separately as a constructor parameter; dropped once its redundancy with
    /// DerivedValue was noticed - see design.md's "Resolved During Review".)
    ///
    /// DerivedValue itself is always supplied at construction, never computed by this type - no
    /// modification/legality engine exists yet (see design.md's Non-Goals), so nothing here can
    /// honestly compute a mutated value. An earlier draft added a ComputeDerivedValue helper plus
    /// per-subtype Create factories that echoed OriginalValue back when a caller-supplied
    /// Func&lt;Ability, bool&gt; classifier deemed every contributor "understood" - removed after
    /// review: with no modification engine, that helper could only ever return OriginalValue or
    /// null, which is not actually deriving anything, and the ad-hoc Func parameter preempted the
    /// real future design (a StatlineFlagRule-style catalogued rule engine, external to these
    /// value types, mirroring how Statline stays pure data and StatlineFlagRuleCatalogue/
    /// AttachedUnitAggregator own the mutation logic instead).</summary>
    public abstract bool IsCaveated { get; }
}

public sealed record ScalarCharacteristicView(
    CharacteristicValue OriginalValue,
    CharacteristicValue? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    public override bool IsCaveated => DerivedValue is null;
}

public sealed record InvulnerableSaveCharacteristicView(
    InvulnerableSave OriginalValue,
    InvulnerableSave? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    public override bool IsCaveated => DerivedValue is null;
}