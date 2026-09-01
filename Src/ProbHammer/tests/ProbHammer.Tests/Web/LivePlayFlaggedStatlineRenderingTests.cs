using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

namespace ProbHammer.Tests.Web;

/// <summary>Exercises `_UnitBlock.cshtml`'s marker/legend rendering (live-play-view's "Flagged
/// Statline Characteristic Rendering", resolve-known-ability-effects) directly against hand-built
/// `AggregateStatlineEntry.Flags` input - the statline-flag-rules matching itself is
/// `AttachedUnitAggregator`'s own concern (see StatlineFlagRuleTests), this covers only how the
/// resulting flags render.</summary>
public class LivePlayFlaggedStatlineRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayFlaggedStatlineRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(AttachedUnitAggregateView view)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var unitBlock = LivePlayModel.BuildUnitBlock(view);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var model = new UnitBlockRenderModel(0, unitBlock, glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    private static readonly Ability Vexilla = new()
    {
        Name = "Vexilla",
        Text = "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
        Scope = AbilityScope.Unit,
        Origin = AbilityOrigin.OptionalGrant
    };

    [Fact]
    public async Task ObjectiveControlFlag_RendersMarkedLabelFlaggedTileAndLegendTrigger()
    {
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 4, 3, 4, 7, 3), // base OC 2 + Vexilla's 1 = 3
            RemainingCount: 1, InitialCount: 1, Loadouts: [],
            Flags: [new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla)]);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC*<")
            .And.Contain("stat-tile-flagged")
            .And.Contain("statline-flag-legend")
            .And.Contain("* Vexilla")
            .And.Contain("Add 1 to the Objective Control characteristic of models in the bearer&#39;s unit.");
    }

    [Fact]
    public async Task UnflaggedObjectiveControl_RendersPlainLabelAndNoLegend()
    {
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 4, 3, 4, 7, 2),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC<").And.NotContain(">OC*<").And.NotContain("statline-flag-legend");
    }

    [Fact]
    public async Task UnitWideSource_KeepsTheSameMarker_AndItsLegendRepeatsInEveryAffectedRun()
    {
        // Two separate runs (differing T, so GroupStatlines can't merge them) both flagged by the
        // exact same source ability - live-play-view's "One source ability keeps the same marker
        // across the whole unit block" and "A unit-wide-scoped source's legend repeats in every
        // affected run".
        var entryA = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Model A",
            Statline: new Statline(6, 4, 3, 4, 7, 3), RemainingCount: 1, InitialCount: 1, Loadouts: [],
            Flags: [new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla)]);
        var entryB = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Model B",
            Statline: new Statline(6, 5, 3, 4, 7, 3), RemainingCount: 1, InitialCount: 1, Loadouts: [],
            Flags: [new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla)]);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entryA, entryB], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC*<");
        html.Should().NotContain(">OC**<"); // same source, same marker in both runs - never a second marker
        var legendOccurrences = html.Split("* Vexilla").Length - 1;
        legendOccurrences.Should().BeGreaterThanOrEqualTo(2); // one legend line per affected run
    }

    [Fact]
    public async Task DistinctSources_GetDistinctMarkers()
    {
        var shieldDome = new Ability
        {
            Name = "Shield Dome", Text = "The bearer has a 5+ invulnerable save.",
            Scope = AbilityScope.Model, Origin = AbilityOrigin.OptionalGrant
        };
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(12, 9, 3, 11, 6, 3) { InSv = new InvulnerableSave(5, 5, false, null) },
            RemainingCount: 1, InitialCount: 1, Loadouts: [],
            Flags:
            [
                new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla),
                new StatlineFlag(StatlineFlagCharacteristic.InvulnerableSave, shieldDome)
            ]);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC*<").And.Contain(">InSv**<");
    }

    [Fact]
    public async Task FullyDeadRun_RendersItsFlaggedTileAndLegendInsideTheSameCollapsedCell()
    {
        // The run-collapse rule (live-play-view's "Statline Section Rendering") hides a collapsed
        // run's flagged tile(s) and legend TOGETHER via CSS scoped to that run's own
        // .statline-cell.col-statline.run-collapsed container - so the legend must render as a
        // descendant of that same cell, not a page-level sibling, for the existing collapse CSS to
        // reach it at all.
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 4, 3, 4, 7, 3),
            RemainingCount: 0, InitialCount: 1, Loadouts: [],
            Flags: [new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla)]);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        var cellStart = html.IndexOf("class=\"statline-cell col-statline run-collapsed\"", StringComparison.Ordinal);
        cellStart.Should().BeGreaterThan(-1);
        var legendStart = html.IndexOf("statline-flag-legend", StringComparison.Ordinal);
        var cellEnd = html.IndexOf("</details>", cellStart, StringComparison.Ordinal);
        legendStart.Should().BeInRange(cellStart, cellEnd);
    }
}