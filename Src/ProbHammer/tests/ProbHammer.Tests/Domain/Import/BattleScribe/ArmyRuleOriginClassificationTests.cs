using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;

namespace ProbHammer.Tests.Domain.Import.BattleScribe;

/// <summary>Regression coverage for classify-known-army-rules' extension to battlescribe-roster-
/// import's "Core Rule Extraction": Origin is Army Rule exactly when the rule's own Name matches
/// one of the known army-wide rule names resolved for the roster's own Faction (see
/// <see cref="ArmyRuleNameLookup"/>), Core Rule otherwise - a new capability for this pipeline,
/// which previously produced only CoreRule Origin with no name-match signal at all. Uses a
/// minimal hand-built roster rather than the big real-excerpt fixture
/// (<c>BattleScribeRosterMapperTests</c>) so the two Rule names under test are independent of
/// that fixture's own Black Templars Faction.</summary>
public class ArmyRuleOriginClassificationTests
{
    private static BsRoster RosterWithRule(string faction, string ruleName) => new()
    {
        Name = "Test Roster",
        Forces =
        [
            new BsRosterForce
            {
                CatalogueName = faction,
                Selections =
                [
                    new BsRosterSelection
                    {
                        Id = "u1",
                        Name = "Test Unit",
                        Type = "unit",
                        Number = 1,
                        Profiles = [new BsRosterProfile { Name = "Test Unit", TypeName = "Unit" }],
                        Rules = [new BsRosterRule { Name = ruleName, Description = "Full text." }]
                    }
                ]
            }
        ]
    };

    [Fact]
    public void ARuleMatchingTheCuratedLookupForTheRostersFaction_HasArmyRuleOrigin()
    {
        var roster = BattleScribeRosterMapper.Map(RosterWithRule("Imperium - Adeptus Astartes - Black Templars", "Templar Vows"));

        var ability = roster.Units[0].Components[0].Datasheet.Abilities.Should().ContainSingle().Subject;
        ability.Name.Should().Be("Templar Vows");
        ability.Origin.Should().Be(AbilityOrigin.ArmyRule);
    }

    [Fact]
    public void ARuleNotMatchingTheCuratedLookup_HasCoreRuleOrigin()
    {
        var roster = BattleScribeRosterMapper.Map(RosterWithRule("Imperium - Adeptus Astartes - Black Templars", "Some Other Rule"));

        var ability = roster.Units[0].Components[0].Datasheet.Abilities.Should().ContainSingle().Subject;
        ability.Name.Should().Be("Some Other Rule");
        ability.Origin.Should().Be(AbilityOrigin.CoreRule);
    }

    [Fact]
    public void AFactionWithNoCuratedEntry_AlwaysClassifiesCoreRule()
    {
        var roster = BattleScribeRosterMapper.Map(RosterWithRule("Some Unmapped Faction", "Anything"));

        var ability = roster.Units[0].Components[0].Datasheet.Abilities.Should().ContainSingle().Subject;
        ability.Origin.Should().Be(AbilityOrigin.CoreRule);
    }
}
