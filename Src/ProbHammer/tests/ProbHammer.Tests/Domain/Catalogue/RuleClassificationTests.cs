using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class RuleClassificationTests
{
    [Fact]
    public void RuleTarget_EachSubtype_IsConstructibleAndDistinct()
    {
        RuleTarget self = new SelfRuleTarget();
        RuleTarget attachedUnit = new AttachedUnitRuleTarget();
        RuleTarget keyword = new KeywordRuleTarget("ADEPTUS ASTARTES");
        RuleTarget unconditional = new UnconditionalRuleTarget();

        self.Should().NotBe(attachedUnit);
        self.Should().NotBe(keyword);
        self.Should().NotBe(unconditional);
        attachedUnit.Should().NotBe(keyword);
        attachedUnit.Should().NotBe(unconditional);
        keyword.Should().NotBe(unconditional);
    }

    [Fact]
    public void RuleClassification_ConstructedWithTargetOnly_DefaultsEffectsToEmptyNotNull()
    {
        var classification = new RuleClassification(new SelfRuleTarget());

        classification.Effects.Should().NotBeNull();
        classification.Effects.Should().BeEmpty();
    }

    [Fact]
    public void RuleClassification_ConstructedWithoutIsCaveated_DefaultsToFalse()
    {
        // The "boring default" RuleClassificationDiff.DefaultClassification reads off this type's
        // own serialization for schema-growth backfill.
        var classification = new RuleClassification(new SelfRuleTarget(), []);

        classification.IsCaveated.Should().BeFalse();
    }
}