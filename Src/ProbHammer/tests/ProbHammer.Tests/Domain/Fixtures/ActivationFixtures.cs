using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class ActivationFixtures
{
    public static ConditionActivations Condition(string abilityName, string conditionText = "") =>
        new(new Dictionary<string, AbilityActivation>
        {
            [abilityName] = new(new HashSet<string> { conditionText }, new Dictionary<int, IReadOnlySet<int>>())
        });

    public static ConditionActivations Choice(string abilityName, int group, params int[] options) =>
        new(new Dictionary<string, AbilityActivation>
        {
            [abilityName] = new(new HashSet<string>(),
                new Dictionary<int, IReadOnlySet<int>> { [group] = new HashSet<int>(options) })
        });
}
