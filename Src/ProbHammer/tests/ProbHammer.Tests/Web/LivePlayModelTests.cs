using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Web;

public class LivePlayModelTests
{
    [Fact]
    public void OnGet_OrdersAttachedUnitsBeforePlainUnits_LargestFirstWithinEachGroup_TiesByName()
    {
        var model = new LivePlayModel();

        model.OnGet();

        // Attached-sourced units (Crusader Squad x2 @ 12 models, Sword Bretheren @ 5) precede
        // plain-Unit-sourced ones (Assault Intercessor / Scout @ 5 models, Impulsor @ 1). Within
        // the tied 12-model attached pair, "High Marshal Helbrecht..." sorts before "Marshal..."
        // ascending; within the tied 5-model plain pair, "Assault Intercessor" sorts before "Scout".
        model.Units.Select(u => u.Name).Should().Equal(
            "Crusader Squad with High Marshal Helbrecht and Crusade Ancient",
            "Crusader Squad with Marshal and Lieutenant",
            "Sword Bretheren Squad with Marshal",
            "Assault Intercessor Squad",
            "Scout Squad",
            "Impulsor");
    }

    [Fact]
    public void BuildUnitBlock_GroupsAdjacentSameComponentEqualValuedStatlines_SeparatesOnValueOrComponentChange()
    {
        var statlineA = new Statline(6, 4, 3, 2, 6, 2);
        var statlineB = new Statline(6, 4, 4, 2, 6, 2); // differs from A by Sv

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: true,
            Statlines:
            [
                new AggregateStatlineEntry("Squad A", "Sword Brother", statlineA, 1, 1, []),
                new AggregateStatlineEntry("Squad A", "Initiate", statlineA, 5, 5, []),
                new AggregateStatlineEntry("Squad A", "Neophyte", statlineB, 4, 4, []),
                new AggregateStatlineEntry("Squad B", "Guardian", statlineB, 1, 1, [])
            ],
            Weapons: [],
            Abilities: [],
            Keywords: new HashSet<string>());

        var block = LivePlayModel.BuildUnitBlock(view);

        // Sword Brother + Initiate share Squad A and statlineA -> one block. Neophyte shares Squad A
        // but has statlineB -> a value change starts a new block. Guardian shares statlineB with
        // Neophyte but belongs to Squad B -> a component change starts a new block even though the
        // value matches the immediately preceding entry.
        block.Statlines.Should().HaveCount(3);
        block.Statlines[0].Entries.Select(e => e.StatlineName).Should().Equal("Sword Brother", "Initiate");
        block.Statlines[1].Entries.Select(e => e.StatlineName).Should().Equal("Neophyte");
        block.Statlines[2].Entries.Select(e => e.StatlineName).Should().Equal("Guardian");
    }

    [Fact]
    public void BuildUnitBlock_RowBindsModelLineAbilities_AndSpansComponentWideAbilitiesAcrossTheirRuns()
    {
        var statlineA = new Statline(6, 4, 3, 2, 6, 2);
        var statlineB = new Statline(6, 4, 4, 2, 6, 2); // differs from A by Sv, so Neophyte gets its own run
        var rowBoundModelAbility = new Ability { Name = "Iron Halo", Text = "...", Scope = AbilityScope.Model };
        var componentWideUnitAbilityA = new Ability { Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit };
        var componentWideModelAbilityB = new Ability { Name = "SUPPORT", Text = "...", Scope = AbilityScope.Model };

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: true,
            Statlines:
            [
                new AggregateStatlineEntry("Squad A", "Sword Brother", statlineA, 1, 1, []),
                new AggregateStatlineEntry("Squad A", "Initiate", statlineA, 5, 5, []),
                new AggregateStatlineEntry("Squad A", "Neophyte", statlineB, 4, 4, []),
                new AggregateStatlineEntry("Squad B", "Guardian", statlineB, 1, 1, [])
            ],
            Weapons: [],
            Abilities:
            [
                new AggregateAbilityEntry("Squad A", "Initiate", rowBoundModelAbility),
                new AggregateAbilityEntry("Squad A", null, componentWideUnitAbilityA),
                new AggregateAbilityEntry("Squad B", null, componentWideModelAbilityB)
            ],
            Keywords: new HashSet<string>());

        var block = LivePlayModel.BuildUnitBlock(view);

        // Row-bound: "Iron Halo" is tied to "Initiate", which merged into run 0 (Sword
        // Brother/Initiate) - it appears on that run only, never on run 1 or 2.
        block.Statlines[0].ModelAbilities.Should().ContainSingle(a => a.Name == "Iron Halo");
        block.Statlines[1].ModelAbilities.Should().BeEmpty();
        block.Statlines[2].ModelAbilities.Should().BeEmpty();

        // Component-wide: Squad A occupies runs 0-1 (Sword Brother/Initiate, then Neophyte) -
        // "Righteous Zeal" spans that whole range, not just run 0.
        block.ComponentAbilitySpans.Should().ContainSingle(s =>
            s.FirstRunIndex == 0 && s.LastRunIndex == 1 && s.UnitAbilities.Single().Name == "Righteous Zeal");

        // Squad B occupies only run 2 (Guardian) - "SUPPORT" spans just that single run, routed
        // into ModelAbilities per its Scope.
        block.ComponentAbilitySpans.Should().ContainSingle(s =>
            s.FirstRunIndex == 2 && s.LastRunIndex == 2 && s.ModelAbilities.Single().Name == "SUPPORT");
    }
}
