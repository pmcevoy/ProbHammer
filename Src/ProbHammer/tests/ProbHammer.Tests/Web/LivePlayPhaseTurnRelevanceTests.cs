using FluentAssertions;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Web;

/// <summary>Exercises LivePlayModel.ExpandedSections/ForcedSections directly against every one of
/// the twelve selection states in live-play-phase-tracker's "Section Relevance By Turn And Phase"
/// table - no DOM/HTTP involved (tasks.md 3.3).</summary>
public class LivePlayPhaseTurnRelevanceTests
{
    private static readonly UnitBlockSection[] AllSections =
        [UnitBlockSection.Statline, UnitBlockSection.Ranged, UnitBlockSection.Melee, UnitBlockSection.Keywords];

    public static IEnumerable<object[]> RowLabelSelections()
    {
        yield return [new PhaseTurnSelection(GameTurn.Mine, null)];
        yield return [new PhaseTurnSelection(GameTurn.Theirs, null)];
    }

    [Theory]
    [MemberData(nameof(RowLabelSelections))]
    public void RowLabelSelection_ExpandsNothing_ForcesAllFour(PhaseTurnSelection selection)
    {
        LivePlayModel.ExpandedSections(selection).Should().BeEmpty();
        LivePlayModel.ForcedSections(selection).Should().BeEquivalentTo(AllSections);
    }

    [Theory]
    [InlineData(GamePhase.Command)]
    [InlineData(GamePhase.Movement)]
    [InlineData(GamePhase.Charge)]
    public void MyTurn_CommandMovementCharge_ForcesOnlyStatline(GamePhase phase)
    {
        var selection = new PhaseTurnSelection(GameTurn.Mine, phase);

        LivePlayModel.ExpandedSections(selection).Should().BeEquivalentTo([UnitBlockSection.Statline]);
        LivePlayModel.ForcedSections(selection).Should().BeEquivalentTo([UnitBlockSection.Statline]);
    }

    [Fact]
    public void MyTurn_Shooting_ExpandsStatlineAndRanged_ForcesMeleeClosedToo()
    {
        var selection = new PhaseTurnSelection(GameTurn.Mine, GamePhase.Shooting);

        LivePlayModel.ExpandedSections(selection).Should()
            .BeEquivalentTo([UnitBlockSection.Statline, UnitBlockSection.Ranged]);
        LivePlayModel.ForcedSections(selection).Should()
            .BeEquivalentTo([UnitBlockSection.Statline, UnitBlockSection.Ranged, UnitBlockSection.Melee]);
    }

    [Fact]
    public void MyTurn_Fight_ExpandsStatlineAndMelee_ForcesRangedClosedToo()
    {
        var selection = new PhaseTurnSelection(GameTurn.Mine, GamePhase.Fight);

        LivePlayModel.ExpandedSections(selection).Should()
            .BeEquivalentTo([UnitBlockSection.Statline, UnitBlockSection.Melee]);
        LivePlayModel.ForcedSections(selection).Should()
            .BeEquivalentTo([UnitBlockSection.Statline, UnitBlockSection.Ranged, UnitBlockSection.Melee]);
    }

    [Theory]
    [InlineData(GamePhase.Command)]
    [InlineData(GamePhase.Movement)]
    [InlineData(GamePhase.Shooting)]
    [InlineData(GamePhase.Charge)]
    public void TheirTurn_CommandMovementShootingCharge_ExpandsOnlyStatline_ForcesAllFour(GamePhase phase)
    {
        var selection = new PhaseTurnSelection(GameTurn.Theirs, phase);

        LivePlayModel.ExpandedSections(selection).Should().BeEquivalentTo([UnitBlockSection.Statline]);
        LivePlayModel.ForcedSections(selection).Should().BeEquivalentTo(AllSections);
    }

    [Fact]
    public void TheirTurn_Fight_ExpandsStatlineAndMelee_ForcesAllFour()
    {
        var selection = new PhaseTurnSelection(GameTurn.Theirs, GamePhase.Fight);

        LivePlayModel.ExpandedSections(selection).Should()
            .BeEquivalentTo([UnitBlockSection.Statline, UnitBlockSection.Melee]);
        LivePlayModel.ForcedSections(selection).Should().BeEquivalentTo(AllSections);
    }

    [Fact]
    public void SectionName_MapsEveryUnitBlockSectionToItsDataSectionToken()
    {
        LivePlayModel.SectionName(UnitBlockSection.Statline).Should().Be("statline");
        LivePlayModel.SectionName(UnitBlockSection.Ranged).Should().Be("ranged");
        LivePlayModel.SectionName(UnitBlockSection.Melee).Should().Be("melee");
        LivePlayModel.SectionName(UnitBlockSection.Keywords).Should().Be("keywords");
    }
}
