using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// A group of models within a Unit that share an identical statline reference and weapon
/// selection. Tracks a live, decrementing remaining count (alive/dead granularity only) alongside
/// the original Count from list composition.
/// </summary>
public sealed class ModelLine
{
    public string StatlineName { get; }
    public IReadOnlyList<string> Weapons { get; }
    public int Count { get; }
    public IReadOnlyList<Ability> Abilities { get; }

    public int RemainingCount { get; private set; }

    public ModelLine(string statlineName, IEnumerable<string> weapons, int count, IEnumerable<Ability>? abilities = null)
    {
        StatlineName = statlineName;
        Weapons = weapons.ToList();
        Count = count;
        Abilities = abilities?.ToList() ?? [];
        RemainingCount = count;
    }

    /// <summary>Removes up to <paramref name="count"/> models as casualties. Clamped at a floor of 0.</summary>
    public void RemoveCasualties(int count) => RemainingCount = Math.Max(0, RemainingCount - count);
}
