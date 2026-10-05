using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Roster;

public class EffectStateTests
{
    private static readonly RuleEffect Oc = new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1);

    private static ClassifiedEffect Effect(string? conditionText = null, ChoiceBranch? branch = null) => new()
    {
        Effect = Oc,
        ResidualConditionBucket = conditionText is null ? ResidualConditionBucket.None : ResidualConditionBucket.Never,
        ConditionText = conditionText,
        ChoiceBranch = branch
    };

    private static AbilityClassification Classify(ClassifiedEffect effect, UsageLimit? usageLimit = null) => new()
    {
        Target = new SelfRuleTarget(),
        Effects = [effect],
        UsageLimit = usageLimit,
        CoverageStatus = CoverageStatus.Complete
    };

    private static AbilityActivation Activation(IEnumerable<string>? conditions = null,
        Dictionary<int, IReadOnlySet<int>>? choices = null) =>
        new(new HashSet<string>(conditions ?? []), choices ?? []);

    [Fact]
    public void AnUnconditionalEffect_IsApplied()
    {
        var effect = Effect();

        EffectStates.Of(Classify(effect), effect, null).Should().Be(EffectState.Applied);
    }

    [Fact]
    public void AnActivatedTextCondition_IsApplied()
    {
        var effect = Effect("while on an objective");

        EffectStates.Of(Classify(effect), effect, Activation(["while on an objective"]))
            .Should().Be(EffectState.Applied);
    }

    [Fact]
    public void ANonActivatedTextCondition_IsNotApplied()
    {
        var effect = Effect("while on an objective");

        EffectStates.Of(Classify(effect), effect, Activation(["some other condition"]))
            .Should().Be(EffectState.NotApplied);
    }

    [Fact]
    public void AnActivatedUsageLimitCondition_IsKeyedByTheEmptyString()
    {
        var effect = Effect();

        EffectStates.Of(Classify(effect, UsageLimit.OncePerBattle), effect, Activation([""]))
            .Should().Be(EffectState.Applied);
    }

    [Fact]
    public void ASelectedOption_IsApplied()
    {
        var effect = Effect(branch: new ChoiceBranch(0, 1));

        EffectStates.Of(Classify(effect), effect, Activation(choices: new() { [0] = new HashSet<int> { 1 } }))
            .Should().Be(EffectState.Applied);
    }

    [Fact]
    public void AnUnselectedOption_OfAGroupWithASelection_IsSuppressed()
    {
        var effect = Effect(branch: new ChoiceBranch(0, 1));

        EffectStates.Of(Classify(effect), effect, Activation(choices: new() { [0] = new HashSet<int> { 0 } }))
            .Should().Be(EffectState.Suppressed);
    }

    [Fact]
    public void AnOption_OfAGroupWithNoSelection_IsNotApplied()
    {
        var effect = Effect(branch: new ChoiceBranch(0, 1));

        EffectStates.Of(Classify(effect), effect, Activation(choices: new() { [1] = new HashSet<int> { 1 } }))
            .Should().Be(EffectState.NotApplied);
        EffectStates.Of(Classify(effect), effect, null).Should().Be(EffectState.NotApplied);
    }

    [Fact]
    public void ASelectedOption_WithItsOwnConditionText_IsAppliedByTheSelectionAlone()
    {
        var effect = Effect("if the target is within 6\"", new ChoiceBranch(0, 0));

        EffectStates.Of(Classify(effect), effect, Activation(choices: new() { [0] = new HashSet<int> { 0 } }))
            .Should().Be(EffectState.Applied);
    }

    [Fact]
    public void ActivationStateForAnotherAbility_IsIgnored()
    {
        var effect = Effect(conditionText: "while on an objective");
        var activations = new ConditionActivations(new Dictionary<string, AbilityActivation>
        {
            ["Other Ability"] = Activation(["while on an objective"])
        });

        EffectStates.Of(Classify(effect), effect, activations.For("Chance for Glory"))
            .Should().Be(EffectState.NotApplied);
        activations.For("other ability").Should().NotBeNull();
    }
}
