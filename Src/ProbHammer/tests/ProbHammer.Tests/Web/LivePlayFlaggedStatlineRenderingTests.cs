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

/// <summary>Exercises `_UnitBlock.cshtml`'s value provenance highlight on Statline tiles
/// (live-play-view's "Value Provenance Highlight") against hand-built statline views - which
/// abilities reach a value is `AttachedUnitAggregator`'s concern (see StatlineFlagRuleTests).</summary>
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

    private static AttachedUnitAggregateView SingleEntryView(Statline statline, int remaining = 1) => new(
        Name: "Test Unit", IsAttachedUnit: false,
        Statlines: [new AggregateStatlineEntry("Test Unit", "Test Unit", statline, remaining, 1, [])],
        Weapons: [], Abilities: [], Keywords: new HashSet<string>());

    [Fact]
    public async Task ModifiedObjectiveControl_RendersTheTileAsAProvenanceTrigger_WithAnUnmarkedLabel()
    {
        // base OC 2 + Vexilla's 1 = 3
        var html = await RenderAsync(SingleEntryView(
            new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla]))));

        html.Should().Contain("class=\"stat-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">OC</span><span class=\"stat-value\">3</span>")
            .And.Contain("provenance-popover")
            .And.Contain(">Vexilla<")
            .And.Contain("Add 1 to the Objective Control characteristic of models in the bearer&#39;s unit.")
            .And.NotContain("OC*")
            .And.NotContain("flag-legend");
    }

    [Fact]
    public async Task UntouchedScalarTiles_RenderPlain()
    {
        var html = await RenderAsync(SingleEntryView(new Statline(6, 4, 3, 5, 6, 1)));

        html.Should().Contain(">M<").And.Contain(">T<").And.Contain(">Sv<").And.Contain(">W<").And.Contain(">Ld<")
            .And.Contain(">OC<").And.NotContain("provenance-tile");
    }

    [Fact]
    public async Task UnitWideSource_HighlightsTheTileInEveryAffectedRun()
    {
        // Two runs (differing T, so GroupStatlines can't merge them) both reached by Vexilla.
        var entryA = new AggregateStatlineEntry("Test Unit", "Model A",
            new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])), 1, 1, []);
        var entryB = new AggregateStatlineEntry("Test Unit", "Model B",
            new Statline(6, 5, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])), 1, 1, []);
        var view = new AttachedUnitAggregateView("Test Unit", false, [entryA, entryB], [], [], new HashSet<string>());

        var html = await RenderAsync(view);

        (html.Split("class=\"stat-tile provenance-tile\"").Length - 1).Should().Be(2);
    }

    [Fact]
    public async Task ModifiedInvulnerableSave_RendersAHighlightedInSvTile()
    {
        var shieldDome = new Ability
        {
            Name = "Shield Dome", Text = "The bearer has a 5+ invulnerable save.",
            Scope = AbilityScope.Model, Origin = AbilityOrigin.OptionalGrant
        };
        var statline = new Statline(12, 9, 3, 11, 6, 2)
        {
            InSv = InvulnerableSaveCharacteristicView.Resolved(InvulnerableSave.None, new InvulnerableSave(5, 5), [shieldDome])
        };

        var html = await RenderAsync(SingleEntryView(statline));

        html.Should().Contain("class=\"stat-tile insv-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">InSv</span><span class=\"stat-value\">5+</span>");
    }

    private static readonly Ability SigilOfCorruption = new()
    {
        Name = "Sigil of Corruption",
        Text = "",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    [Fact]
    public async Task AnAbilityWithNoText_IsAPlainLineInThePopover_NotANestedTrigger()
    {
        var html = await RenderAsync(SingleEntryView(
            new Statline(6, 4, ScalarCharacteristicView.Resolved(3, 4, [SigilOfCorruption]), 5, 6, 1)));

        html.Should().Contain("<tr class=\"provenance-line\"><td>Sigil of Corruption</td><td>4+</td></tr>");
        html.Should().NotContain("class=\"ability-name-line\"");
    }

    [Fact]
    public async Task FullyDeadRun_RendersItsHighlightedTileInsideTheSameCollapsedCell()
    {
        var html = await RenderAsync(SingleEntryView(
            new Statline(6, 4, 3, 4, 7, ScalarCharacteristicView.Resolved(2, 3, [Vexilla])), remaining: 0));

        var cellStart = html.IndexOf("class=\"statline-cell col-statline run-collapsed\"", StringComparison.Ordinal);
        cellStart.Should().BeGreaterThan(-1);
        var tileStart = html.IndexOf("provenance-tile", StringComparison.Ordinal);
        var cellEnd = html.IndexOf("</details>", cellStart, StringComparison.Ordinal);
        tileStart.Should().BeInRange(cellStart, cellEnd);
    }

    private static readonly Ability AuricMantle = new()
    {
        Name = "Auric Mantle",
        Text = "Shield-Captain or Blade Champion model only. Add 2 to the bearer's Wounds characteristic.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.Enhancement
    };

    [Fact]
    public async Task CaveatedScalarTile_RendersHighlighted_WithTheDatasheetValueAndNoTotal()
    {
        var html = await RenderAsync(SingleEntryView(
            new Statline(6, 6, 2, ScalarCharacteristicView.Caveated(6, AuricMantle), 7, 2)));

        html.Should().Contain("class=\"stat-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">W</span><span class=\"stat-value\">6</span>")
            .And.Contain("<th>Datasheet</th>")
            .And.Contain(">✦ Auric Mantle<")
            .And.NotContain("provenance-result")
            .And.NotContain("W*");
    }

    [Fact]
    public async Task ANotAddedInvulnerableSaveOnAUnitWithoutOne_RendersAHighlightedTileShowingNoSave()
    {
        var waaagh = new Ability
        {
            Name = "Waaagh!", Text = "Waaagh! test text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };
        var entry = new AggregateStatlineEntry("Boyz", "Boy", new Statline(6, 5, 5, 1, 7, 2), 10, 10, [],
            NotAppliedEffects:
            [
                new NotAppliedStatlineEffect(waaagh, new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5)),
                    new EffectCondition(null, null, "While the unit is riled up", false))
            ]);

        var html = await RenderAsync(new AttachedUnitAggregateView("Boyz", false, [entry], [], [], new HashSet<string>()));

        html.Should().Contain("class=\"stat-tile insv-tile provenance-tile provenance-cond\"")
            .And.Contain("<span class=\"stat-label\">InSv</span><span class=\"stat-value\">–</span>")
            .And.Contain("While the unit is riled up; not added");
    }
}
