namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A Statline's invulnerable save, split by attack type. <see cref="Caveated"/> marks a
/// value whose source text carried a footnote/restriction this layer could not itself interpret -
/// <see cref="MeleeInSv"/>/<see cref="RangedInSv"/> then hold the one value known without that
/// interpretation (see BsdataDatasheetMapper's InSv resolution), and <see cref="CaveatAbility"/>
/// carries the specific linked Ability a future interpretation step would read. The 4-argument
/// constructor guards the real invariant (a caveated value always carries an ability) for every
/// production construction path; an object initializer on the parameterless constructor, or a
/// `with` expression, can still bypass it - the same class of record-validation limitation this
/// project already accepts elsewhere (see WeaponProfile's Skill/with-desync notes).</summary>
public sealed record InvulnerableSave
{
    public int MeleeInSv { get; init; }
    public int RangedInSv { get; init; }
    public bool Caveated { get; init; }
    public Ability? CaveatAbility { get; init; }

    public InvulnerableSave()
    {
    }

    public InvulnerableSave(int meleeInSv, int rangedInSv, bool caveated, Ability? caveatAbility)
    {
        if (caveated && caveatAbility is null)
            throw new ArgumentException(
                "A caveated InvulnerableSave must carry a CaveatAbility.", nameof(caveatAbility));

        MeleeInSv = meleeInSv;
        RangedInSv = rangedInSv;
        Caveated = caveated;
        CaveatAbility = caveatAbility;
    }

    /// <summary>Uniform saves (melee == ranged, non-caveated) are the common case - mirrors
    /// DiceExpression's implicit int conversion so a plain digit reads naturally at call sites.</summary>
    public static implicit operator InvulnerableSave(int uniformValue) =>
        new(uniformValue, uniformValue, caveated: false, caveatAbility: null);
}
