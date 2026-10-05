namespace ProbHammer.Core.Domain.Roster;

/// <summary>Player-set activation state of a combat unit's conditional ability effects, keyed by
/// ability name (case-insensitive). Never changed by the system itself.</summary>
public sealed class ConditionActivations
{
    private readonly IReadOnlyDictionary<string, AbilityActivation> _byAbility;

    public ConditionActivations(IReadOnlyDictionary<string, AbilityActivation> byAbility) =>
        _byAbility = new Dictionary<string, AbilityActivation>(byAbility, StringComparer.OrdinalIgnoreCase);

    public static readonly ConditionActivations Empty = new(new Dictionary<string, AbilityActivation>());

    public IReadOnlyDictionary<string, AbilityActivation> ByAbility => _byAbility;

    public AbilityActivation? For(string abilityName) => _byAbility.GetValueOrDefault(abilityName);
}

/// <summary>One ability's activated condition texts (the empty string for a condition with no text)
/// and, per choice group index, its selected option indexes.</summary>
public sealed record AbilityActivation(
    IReadOnlySet<string> Conditions,
    IReadOnlyDictionary<int, IReadOnlySet<int>> Choices);
