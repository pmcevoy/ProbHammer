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
            // base OC 2 + Vexilla's 1 = 3
            Statline: new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        // The marker renders in its own span OUTSIDE the ability-name-line pill, not baked into the
        // trigger's own text - direct user preference.
        html.Should().Contain(">OC*<")
            .And.Contain("stat-tile-flagged")
            .And.Contain("statline-flag-legend")
            .And.Contain("flag-legend-marker\">*</span>")
            .And.Contain(">Vexilla<")
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
            Statline: new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var entryB = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Model B",
            Statline: new Statline(6, 5, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entryA, entryB], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC*<");
        html.Should().NotContain(">OC**<"); // same source, same marker in both runs - never a second marker
        var legendOccurrences = html.Split("flag-legend-marker\">*</span>").Length - 1;
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
        var shieldDomeValue = new InvulnerableSave(5, 5);
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            // base OC 2 + Vexilla's 1 = 3; Impulsor has no base InSv before Shield Dome grants one
            Statline: new Statline(12, 9, 3, 11, 6, ScalarCharacteristicView.Resolved(2, 3, [Vexilla]))
            {
                InSv = InvulnerableSaveCharacteristicView.Resolved(InvulnerableSave.None, shieldDomeValue, [shieldDome])
            },
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">OC*<").And.Contain(">InSv**<");
    }

    private static readonly Ability SigilOfCorruption = new()
    {
        Name = "Sigil of Corruption",
        Text = "",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    [Fact]
    public async Task StructuralModifierFlaggedSave_RendersMarkedLabelFlaggedTileAndLegendTrigger()
    {
        // resolve-structured-characteristic-modifiers: generalizes the same marker/legend
        // mechanism to any ScalarCharacteristicView-backed tile, not only OC - Sv here, with a
        // source ability that has no descriptive text of its own (live-play-view's "no descriptive
        // text" scenario).
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 4, ScalarCharacteristicView.Resolved(3, 4, [SigilOfCorruption]), 5, 6, 1),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">Sv*<")
            .And.Contain("stat-tile-flagged")
            .And.Contain("statline-flag-legend")
            .And.Contain("flag-legend-marker\">*</span>")
            .And.Contain(">Sigil of Corruption<");
        // no descriptive text of its own - renders as plain, non-interactive text, not a popover
        // trigger button (live-play-view's "no descriptive text" scenario).
        html.Should().Contain("<span class=\"ability-name-line\">Sigil of Corruption</span>")
            .And.NotContain("popovertarget");
    }

    [Fact]
    public async Task UnflaggedScalarTiles_RenderPlainLabelsWithNoMarker()
    {
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 4, 3, 5, 6, 1),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">M<").And.Contain(">T<").And.Contain(">Sv<").And.Contain(">W<").And.Contain(">Ld<")
            .And.NotContain(">M*<").And.NotContain(">Sv*<").And.NotContain("stat-tile-flagged");
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
            Statline: new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])),
            RemainingCount: 0, InitialCount: 1, Loadouts: []);
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

    private static readonly Ability AuricMantle = new()
    {
        Name = "Auric Mantle",
        Text = "Shield-Captain or Blade Champion model only. Add 2 to the bearer's Wounds characteristic.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.Enhancement
    };

    [Fact]
    public async Task CaveatedScalarTile_RendersMarkedLabelAndLegendButNoAmberBackground()
    {
        // characteristic-modifier-caveats: a still-caveated Scalar tile shows the plain catalogue
        // Value (IsCaveated selects OriginalValue), never a computed result - painting it amber
        // would wrongly suggest the shown number already accounts for the linked ability. Amber
        // (stat-tile-flagged) is reserved for a RESOLVED run (see the Vexilla/Sigil of Corruption
        // tests above, both ScalarCharacteristicView.Resolved). Per direct user feedback reviewing a
        // real caveated "Auric Mantle" W tile live.
        var entry = new AggregateStatlineEntry(
            ComponentName: "Test Unit", StatlineName: "Test Unit",
            Statline: new Statline(6, 6, 2, ScalarCharacteristicView.Caveated(6, AuricMantle), 7, 2),
            RemainingCount: 1, InitialCount: 1, Loadouts: []);
        var view = new AttachedUnitAggregateView(
            Name: "Test Unit", IsAttachedUnit: false, Statlines: [entry], Weapons: [], Abilities: [],
            Keywords: new HashSet<string>());

        var html = await RenderAsync(view);

        html.Should().Contain(">W*<")
            .And.Contain(">6<")
            .And.NotContain("stat-tile-flagged")
            .And.Contain("statline-flag-legend")
            .And.Contain("flag-legend-marker\">*</span>")
            .And.Contain(">✦ Auric Mantle<");
    }
}