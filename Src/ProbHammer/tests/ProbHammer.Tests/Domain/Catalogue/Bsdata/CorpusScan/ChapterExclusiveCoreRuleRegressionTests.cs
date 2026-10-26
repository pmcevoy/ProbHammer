using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>Explicit regression checks against the live clone: every Space Marine chapter's Army Rule
/// is Combat Doctrines since the 2026 codex, and the retired Oath of Moment no longer resolves.</summary>
public class ChapterExclusiveCoreRuleRegressionTests
{
    [Theory(Explicit = true)]
    [InlineData("Imperium - Black Templars.json", "Black Templars")]
    [InlineData("Imperium - Salamanders.json", "Salamanders")]
    public void ScoutSquad_GetsCombatDoctrinesAsItsArmyRule_NotOathOfMoment(string startingFile, string chapter)
    {
        var catalogue = ResolvedBsdataCatalogue.Build(LiveClone.RequireSource(), startingFile);

        var scoutSquad =
            catalogue.ResolveDatasheet("Scout Squad", ArmyRuleNameLookup.Resolve(["Space Marines", chapter]));

        scoutSquad.Abilities.Should()
            .ContainSingle(a => a.Name == "Combat Doctrines" && a.Origin == AbilityOrigin.ArmyRule);
        scoutSquad.Abilities.Should().NotContain(a => a.Name == "Oath of Moment");
    }
}