using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class AttachedUnitTests
{
    [Fact]
    public void DefaultOneLeaderOneSupport_AttachedCollectionContainsBothUnits()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        attachedUnit.Attached.Should().HaveCount(2);
    }

    [Fact]
    public void ExceptionWithMoreThanOneLeader_IsNotRejectedOrRestructured()
    {
        var attachedUnit = AttachedUnitFixtures.TwoLeadersPlusSupport();

        attachedUnit.Attached.Should().HaveCount(3);
    }

    [Fact]
    public void AttachmentSource_IsNotTracked()
    {
        // AttachedUnit's shape has no field recording whether a Unit was attached via Leader or
        // Support - Attached is a flat, undifferentiated collection.
        var properties = typeof(AttachedUnit).GetProperties().Select(p => p.Name);

        properties.Should().NotContain(n => n.Contains("Leader") || n.Contains("Support"));
    }

    [Fact]
    public void KeywordUnion_ReflectsAllPresentComponents()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        var keywords = KeywordResolution.EffectiveKeywords(attachedUnit);

        keywords.Should().Contain("BATTLELINE"); // from the Bodyguard
        keywords.Should().Contain("CHARACTER");  // from the attached Leader
    }

    [Fact]
    public void KeywordUnion_ChangesWhenAComponentIsRemoved()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();
        var support = attachedUnit.Attached.Single(u => u.Datasheet.Name == "Servitor");

        var before = KeywordResolution.EffectiveKeywords(attachedUnit);
        before.Should().Contain("SERVITOR");

        foreach (var modelLine in support.ModelLines)
            modelLine.RemoveCasualties(modelLine.Count);

        var after = KeywordResolution.EffectiveKeywords(attachedUnit);
        after.Should().NotContain("SERVITOR");
        after.Should().Contain("BATTLELINE"); // Bodyguard's own keywords are untouched
        after.Should().Contain("CHARACTER");  // Leader's own keywords are untouched
    }

    [Fact]
    public void ModelLevelChecks_DoNotSeeOtherComponentsKeywords()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        // The Bodyguard's own Keywords do not include CHARACTER, even though the AttachedUnit's
        // effective union does (via the attached Leader).
        attachedUnit.Bodyguard.Datasheet.Keywords.Should().NotContain("CHARACTER");
        KeywordResolution.EffectiveKeywords(attachedUnit).Should().Contain("CHARACTER");
    }

    [Fact]
    public void ToughnessResolution_UsesBodyguardWhenBodyguardModelsRemain()
    {
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();

        // Bodyguard (Crusader Squad "Initiate") is T4; attached Leader ("Chaplain") is T5, higher -
        // the rule must still pick the Bodyguard's Toughness while any Bodyguard model remains.
        var toughness = ToughnessResolution.ResolveDefendingToughness(attachedUnit);

        toughness.Should().Be(4);
    }

    [Fact]
    public void ToughnessResolution_FallsBackToAttachedWhenBodyguardIsDestroyed()
    {
        var attachedUnit = AttachedUnitFixtures.BodyguardDestroyed();

        var toughness = ToughnessResolution.ResolveDefendingToughness(attachedUnit);

        toughness.Should().Be(5); // highest of remaining Leader (T5) / Support (T3)
    }
}
