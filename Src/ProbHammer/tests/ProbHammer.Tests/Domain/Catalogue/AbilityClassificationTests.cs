using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class AbilityClassificationTests
{
    private static readonly ClassifiedEffect PlainEffect = new()
    {
        Effect = new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1),
        ResidualConditionBucket = ResidualConditionBucket.None
    };

    private static AbilityClassification Classify(ClassifiedEffect effect, UsageLimit? usageLimit = null,
        GameTurn? turnOwnership = null, IReadOnlyList<GamePhase>? phases = null) =>
        new()
        {
            Target = new SelfRuleTarget(),
            Effects = [effect],
            Phases = phases ?? [],
            UsageLimit = usageLimit,
            TurnOwnership = turnOwnership,
            CoverageStatus = CoverageStatus.Complete
        };

    [Fact]
    public void AnEffectWithNoConditionOfAnyKind_IsUnconditional()
    {
        var classification = Classify(PlainEffect);

        classification.IsUnconditional(PlainEffect).Should().BeTrue();
    }

    [Fact]
    public void PhasesAlone_DoNotMakeAnEffectConditional()
    {
        var classification = Classify(PlainEffect, phases: [GamePhase.Shooting, GamePhase.Fight]);

        classification.IsUnconditional(PlainEffect).Should().BeTrue();
    }

    [Theory]
    [InlineData(ResidualConditionBucket.EvaluableNow)]
    [InlineData(ResidualConditionBucket.Never)]
    public void AResidualCondition_MakesAnEffectConditional(ResidualConditionBucket bucket)
    {
        var effect = PlainEffect with { ResidualConditionBucket = bucket, ConditionText = "While on an objective" };

        Classify(effect).IsUnconditional(effect).Should().BeFalse();
    }

    [Fact]
    public void AUsageLimit_MakesAnEffectConditional_EvenWithNoResidualCondition()
    {
        var classification = Classify(PlainEffect, usageLimit: UsageLimit.OncePerBattle);

        classification.IsUnconditional(PlainEffect).Should().BeFalse();
    }

    [Fact]
    public void ATurnRestriction_MakesAnEffectConditional()
    {
        var classification = Classify(PlainEffect, turnOwnership: GameTurn.Mine);

        classification.IsUnconditional(PlainEffect).Should().BeFalse();
    }

    [Fact]
    public void AChoiceBranchEffect_IsConditional()
    {
        var effect = PlainEffect with { ChoiceBranch = new ChoiceBranch(0, 1) };

        Classify(effect).IsUnconditional(effect).Should().BeFalse();
    }

    [Fact]
    public void KeywordTargets_WithEqualKeywordLists_AreEqual()
    {
        new KeywordRuleTarget(["LEAGUES OF VOTANN", "INFANTRY"])
            .Should().Be(new KeywordRuleTarget(["LEAGUES OF VOTANN", "INFANTRY"]));
    }
}
