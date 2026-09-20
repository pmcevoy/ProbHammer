using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

/// <summary>Covers `army-roster-enrichment`'s Detachment Rule Keyword Target Resolution requirement
/// - every scenario matches its own spec.md scenario name.</summary>
public class DetachmentRuleInboundAbilityResolverTests
{
    private const string FaithFuelledResolveText = "Friendly SWORD BRETHREN SQUAD units have +1 OC.";

    private static readonly RuleClassificationBaseline KeywordBaseline = RuleClassificationBaseline.FromEntries(
    [
        new RuleClassificationBaselineEntry(
            Text: FaithFuelledResolveText,
            Target: new KeywordRuleTarget("SWORD BRETHREN SQUAD"),
            Effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
    ]);

    private static ResolvedDetachment MarshalsHousehold() =>
        new("Marshal's Household", [new DetachmentRule("Faith-Fuelled Resolve", FaithFuelledResolveText)]);

    [Fact]
    public void AKeywordMatchedRule_AttachesToEveryUnitCarryingThatKeyword()
    {
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [MarshalsHousehold()], KeywordBaseline);

        swordBrethren.InboundAbilities.Should().ContainSingle(a =>
            a.Name == "Faith-Fuelled Resolve" &&
            a.Text == FaithFuelledResolveText &&
            a.Scope == AbilityScope.Unit &&
            a.Origin == AbilityOrigin.DetachmentRule);
    }

    [Fact]
    public void ANonMatchingUnit_IsUnaffected()
    {
        var nonMatching = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();

        DetachmentRuleInboundAbilityResolver.Apply([nonMatching], [MarshalsHousehold()], KeywordBaseline);

        nonMatching.InboundAbilities.Should().BeEmpty();
    }

    [Fact]
    public void AnAttachedUnit_GainsAKeywordMatchedRule_ViaAnyPresentComponent()
    {
        var bodyguard = UnitFixtures.CrusaderSquadMixedLoadout();
        var attachedSwordBrethren = UnitFixtures.SwordBrethrenSquadUniform();
        var attachedUnit = new AttachedUnit(bodyguard, [attachedSwordBrethren]);

        DetachmentRuleInboundAbilityResolver.Apply([attachedUnit], [MarshalsHousehold()], KeywordBaseline);

        attachedUnit.InboundAbilities.Should().ContainSingle(a => a.Name == "Faith-Fuelled Resolve");
    }

    [Fact]
    public void ADetachmentRuleWithNoBaselineEntry_AttachesNothing()
    {
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();
        var unbaselined = new ResolvedDetachment("Unknown Detachment",
            [new DetachmentRule("Unbaselined Rule", "Some rule text with no baseline entry.")]);

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [unbaselined], KeywordBaseline);

        swordBrethren.InboundAbilities.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(SelfRuleTarget))]
    [InlineData(typeof(UnconditionalRuleTarget))]
    public void ADetachmentRuleClassifiedWithANonKeywordTarget_AttachesNothing(Type targetType)
    {
        const string text = "Some other Detachment rule text.";
        var target = (RuleTarget)Activator.CreateInstance(targetType)!;
        var baseline = RuleClassificationBaseline.FromEntries(
        [
            new RuleClassificationBaselineEntry(Text: text, Target: target, Effects: [])
        ]);
        var detachment = new ResolvedDetachment("Some Detachment", [new DetachmentRule("Some Rule", text)]);
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [detachment], baseline);

        swordBrethren.InboundAbilities.Should().BeEmpty();
    }
}
