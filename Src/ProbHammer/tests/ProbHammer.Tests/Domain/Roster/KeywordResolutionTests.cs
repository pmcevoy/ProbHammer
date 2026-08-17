using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class KeywordResolutionTests
{
    [Fact]
    public void UnitLevelUnion_IncludesAPresentModelLinesOwnKeyword()
    {
        var unit = UnitFixtures.ChaosSpaceMarineSquadWithPsyker();

        unit.Datasheet.Keywords.Should().NotContain("PSYKER");
        KeywordResolution.EffectiveKeywords(unit).Should().Contain("PSYKER");
    }

    [Fact]
    public void ModelLevelCheck_SucceedsForTheTaggedModelLine_FailsForASiblingModelLine()
    {
        var unit = UnitFixtures.ChaosSpaceMarineSquadWithPsyker();

        var gunner = unit.ModelLines.Single(ml => ml.StatlineName == "Chaos Space Marine Gunner");
        var reaper = unit.ModelLines.Single(ml => ml.StatlineName == "Chaos Space Marine Reaper");

        gunner.Keywords.Should().Contain("PSYKER");
        reaper.Keywords.Should().NotContain("PSYKER");
    }

    [Fact]
    public void UnitLevelUnion_DropsAModelLinesKeyword_OnceItHasNoModelsRemaining()
    {
        var unit = UnitFixtures.ChaosSpaceMarineSquadWithPsyker();
        var gunner = unit.ModelLines.Single(ml => ml.StatlineName == "Chaos Space Marine Gunner");

        KeywordResolution.EffectiveKeywords(unit).Should().Contain("PSYKER");

        gunner.RemoveCasualties(gunner.Count);

        KeywordResolution.EffectiveKeywords(unit).Should().NotContain("PSYKER");
    }
}
