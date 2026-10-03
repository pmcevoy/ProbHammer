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

    private static readonly AbilityClassificationCatalogue KeywordClassifications = ClassificationFixtures.Catalogue(
    [
        ClassificationFixtures.Entry(
            text: FaithFuelledResolveText,
            target: new KeywordRuleTarget(["SWORD BRETHREN SQUAD"]),
            effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
    ]);

    private static ResolvedDetachment MarshalsHousehold() =>
        new("Marshal's Household", [new DetachmentRule("Faith-Fuelled Resolve", FaithFuelledResolveText)]);

    [Fact]
    public void AKeywordMatchedRule_AttachesToEveryUnitCarryingThatKeyword()
    {
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [MarshalsHousehold()], KeywordClassifications);

        swordBrethren.InboundAbilities.Should().ContainSingle(a =>
            a.Name == "Faith-Fuelled Resolve" &&
            a.Text == FaithFuelledResolveText &&
            a.Scope == AbilityScope.Unit &&
            a.Origin == AbilityOrigin.DetachmentRule);
    }

    [Fact]
    public void AMultiKeywordTarget_RequiresEveryKeyword()
    {
        const string text = "Friendly INFANTRY SWORD BRETHREN SQUAD units have +1 OC.";
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(
                text: text,
                target: new KeywordRuleTarget(["infantry", "SWORD BRETHREN SQUAD"]),
                effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
        ]);
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();
        var assaultIntercessors = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var detachment = new ResolvedDetachment("Some Detachment", [new DetachmentRule("Some Rule", text)]);

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren, assaultIntercessors], [detachment],
            classifications);

        swordBrethren.InboundAbilities.Should().ContainSingle(a => a.Name == "Some Rule");
        assaultIntercessors.InboundAbilities.Should().BeEmpty();
    }

    [Fact]
    public void ANonMatchingUnit_IsUnaffected()
    {
        var nonMatching = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();

        DetachmentRuleInboundAbilityResolver.Apply([nonMatching], [MarshalsHousehold()], KeywordClassifications);

        nonMatching.InboundAbilities.Should().BeEmpty();
    }

    [Fact]
    public void AnAttachedUnit_GainsAKeywordMatchedRule_ViaAnyPresentComponent()
    {
        var bodyguard = UnitFixtures.CrusaderSquadMixedLoadout();
        var attachedSwordBrethren = UnitFixtures.SwordBrethrenSquadUniform();
        var attachedUnit = new AttachedUnit(bodyguard, [attachedSwordBrethren]);

        DetachmentRuleInboundAbilityResolver.Apply([attachedUnit], [MarshalsHousehold()], KeywordClassifications);

        attachedUnit.InboundAbilities.Should().ContainSingle(a => a.Name == "Faith-Fuelled Resolve");
    }

    [Fact]
    public void ADetachmentRuleWithNoClassification_AttachesNothing()
    {
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();
        var unclassified = new ResolvedDetachment("Unknown Detachment",
            [new DetachmentRule("Unclassified Rule", "Some rule text with no classification.")]);

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [unclassified], KeywordClassifications);

        swordBrethren.InboundAbilities.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(SelfRuleTarget))]
    [InlineData(typeof(UnconditionalRuleTarget))]
    public void ADetachmentRuleClassifiedWithANonKeywordTarget_AttachesNothing(Type targetType)
    {
        const string text = "Some other Detachment rule text.";
        var target = (RuleTarget)Activator.CreateInstance(targetType)!;
        var classifications = ClassificationFixtures.Catalogue(
        [
            ClassificationFixtures.Entry(text: text, target: target, effects: [])
        ]);
        var detachment = new ResolvedDetachment("Some Detachment", [new DetachmentRule("Some Rule", text)]);
        var swordBrethren = UnitFixtures.SwordBrethrenSquadUniform();

        DetachmentRuleInboundAbilityResolver.Apply([swordBrethren], [detachment], classifications);

        swordBrethren.InboundAbilities.Should().BeEmpty();
    }
}