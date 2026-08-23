using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>A Datasheet selected into a specific army list, with model-lines carrying statline
/// reference, chosen weapons, and count.</summary>
public sealed class Unit : ICombatUnit
{
    public Datasheet Datasheet { get; }
    public IReadOnlyList<Ability> Enhancements { get; }
    public IReadOnlyList<ModelLine> ModelLines { get; }

    public Unit(Datasheet datasheet, IEnumerable<Ability> enhancements, IEnumerable<ModelLine> modelLines)
    {
        Datasheet = datasheet;
        Enhancements = enhancements.ToList();
        ModelLines = modelLines.ToList();
    }

    /// <summary>A Unit is a single-component combat unit for aggregate-view purposes.</summary>
    public IReadOnlyList<Unit> Components => [this];

    public string Name => Datasheet.Name;

    public bool IsPresent => ModelLines.Any(ml => ml.RemainingCount > 0);
}
