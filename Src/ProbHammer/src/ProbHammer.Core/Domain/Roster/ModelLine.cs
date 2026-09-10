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

    /// <summary>The import's own raw name for this specific model-line, before resolution against
    /// the catalogue's declared Statline names (e.g. "Plague Champion", "Custodian Warden w/
    /// Vexilla") - distinct from <see cref="StatlineName"/>, which is always one of the Datasheet's
    /// own declared Statline names. Defaults to <see cref="StatlineName"/> when a pipeline has no
    /// such raw text of its own to offer (or the text is identical after resolution, e.g. the GW-app
    /// text pipeline's "Initiate" model groups) - a consumer distinguishing sibling loadouts never
    /// needs to special-case "not available".</summary>
    public string DisplayName { get; }

    public IReadOnlyList<string> Weapons { get; }
    public int Count { get; }
    public IReadOnlyList<Ability> Abilities { get; }

    /// <summary>Keywords scoped to this specific model-line, distinct from its Datasheet's
    /// Keywords - e.g. a keyword belonging to only one named individual within a shared statline.
    /// Named individuals sharing a statline are modeled as separate model-lines when a keyword
    /// differs.</summary>
    public IReadOnlySet<string> Keywords { get; }

    public int RemainingCount { get; private set; }

    public ModelLine(string statlineName, IEnumerable<string> weapons, int count,
        IEnumerable<Ability>? abilities = null, IEnumerable<string>? keywords = null, string? displayName = null)
    {
        StatlineName = statlineName;
        DisplayName = displayName ?? statlineName;
        Weapons = weapons.ToList();
        Count = count;
        Abilities = abilities?.ToList() ?? [];
        Keywords = new HashSet<string>(keywords ?? [], StringComparer.OrdinalIgnoreCase);
        RemainingCount = count;
    }

    /// <summary>Sets the remaining count directly, clamped to <c>[0, Count]</c>. The single primitive
    /// both casualty removal and mis-tap correction reduce to.</summary>
    public void SetRemainingCount(int value) => RemainingCount = Math.Clamp(value, 0, Count);

    /// <summary>Removes up to <paramref name="count"/> models as casualties. Clamped at a floor of 0.</summary>
    public void RemoveCasualties(int count) => SetRemainingCount(RemainingCount - count);
}