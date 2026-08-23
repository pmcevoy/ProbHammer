using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

namespace ProbHammer.Tests.Web;

/// <summary>Exercises `_UnitBlock.cshtml`'s unit-header status glyphs and Battle-shocked Objective
/// Control tile rendering (half-strength-and-battleshock-indicators), against real `ICombatUnit`
/// fixtures rather than a bare `AttachedUnitAggregateView` - unlike
/// `LivePlayInvulnerableSaveRenderingTests`, the branching under test here (single-model vs.
/// multi-model glyph mode, Battle-shocked OC rendering) reads `ICombatUnit`-level state
/// (`HalfStrengthResolution`, `IsBattleShocked`) that only exists via
/// `LivePlayModel.BuildUnitBlock(view, unit)`'s two-argument overload.</summary>
public class LivePlayHalfStrengthAndBattleShockRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayHalfStrengthAndBattleShockRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(ICombatUnit unit)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var view = AttachedUnitAggregator.Build(unit);
        var unitBlock = LivePlayModel.BuildUnitBlock(view, unit);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var model = new UnitBlockRenderModel(0, unitBlock, glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    [Fact]
    public async Task SingleModelUnit_RendersAnInteractiveHalfStrengthButton_ReflectingTheOverride()
    {
        var unit = AttachedUnitFixtures.LeaderUnit();

        var pristineHtml = await RenderAsync(unit);
        pristineHtml.Should().Contain("button").And.Contain("status-glyph-halfstrength");
        pristineHtml.Should().NotContain("status-glyph-halfstrength is-active");

        unit.IsHalfStrengthOverride = true;
        var setHtml = await RenderAsync(unit);
        setHtml.Should().Contain("status-glyph-halfstrength is-active");
    }

    [Fact]
    public async Task MultiModelUnit_AboveHalfStrength_RendersTheHalfStrengthGlyphHidden()
    {
        // The <span> variant is always in the markup (matching .filtered-badge's own established
        // "always present, hidden attribute toggles visibility" convention on this page) - only the
        // `hidden` attribute (razor's conditional-boolean-attribute rendering, the same mechanism
        // .casualty-reset-btn's own `hidden="@(!unit.HasCasualties)"` already relies on) controls
        // whether it's actually shown.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader(); // 5 full-health models

        var html = await RenderAsync(unit);

        html.Should().Contain("<span class=\"filter-flag status-glyph-halfstrength is-active\" hidden");
    }

    [Fact]
    public async Task MultiModelUnit_AtOrBelowHalfStrength_RendersAPassiveVisibleAmberGlyph()
    {
        // Sergeant (1) + troopers (4) = 5; half = floor(5/2) = 2.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var troopers = unit.ModelLines.Single(ml => ml.StatlineName == "Assault Intercessor");
        troopers.SetRemainingCount(1); // sergeant (1) + troopers (1) = 2 <= 2

        var html = await RenderAsync(unit);

        html.Should().Contain("status-glyph-halfstrength is-active");
        // The `hidden` attribute is razor-omitted once the condition is false - see
        // MultiModelUnit_AboveHalfStrength_RendersTheHalfStrengthGlyphHidden's own comment.
        html.Should().NotContain("<span class=\"filter-flag status-glyph-halfstrength is-active\" hidden");
        // The multi-model case is a non-interactive <span>, never a <button> - see the spec's
        // "Unit Header Status Glyphs" requirement.
        html.Should().NotContain("<button type=\"button\" class=\"filter-flag status-glyph-halfstrength");
    }

    [Fact]
    public async Task TheBattleShockGlyph_AlwaysRenders_RightmostOfTheStatusGlyphs()
    {
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();

        var html = await RenderAsync(unit);

        html.Should().Contain("status-glyph-battleshock");
        // For a >= 2 starting-strength unit above half-strength, the half-strength glyph doesn't
        // render at all, so the Battle-shock glyph trivially being present is enough here; the
        // AttachedUnit test below covers relative ordering when both glyphs are present.
    }

    [Fact]
    public async Task BothGlyphsPresent_BattleShockRendersAfterHalfStrengthInMarkupOrder()
    {
        var unit = AttachedUnitFixtures.LeaderUnit(); // single-model - half-strength glyph always renders
        unit.IsBattleShocked = true;

        var html = await RenderAsync(unit);

        var halfStrengthIndex = html.IndexOf("status-glyph-halfstrength", StringComparison.Ordinal);
        var battleShockIndex = html.IndexOf("status-glyph-battleshock", StringComparison.Ordinal);
        halfStrengthIndex.Should().BeGreaterThan(-1);
        battleShockIndex.Should().BeGreaterThan(halfStrengthIndex);
    }

    [Fact]
    public async Task NotBattleShocked_OcTileRendersTheOrdinaryNumericValue()
    {
        var unit = AttachedUnitFixtures.LeaderUnit(); // Oc: 1

        var html = await RenderAsync(unit);

        html.Should().Contain(">OC<").And.Contain(">1<").And.NotContain("oc-battleshock-icon");
    }

    [Fact]
    public async Task BattleShocked_OcTileRendersTheFlaggedTileAndGlyph_NotTheNumericValue()
    {
        var unit = AttachedUnitFixtures.LeaderUnit(); // Oc: 1
        unit.IsBattleShocked = true;

        var html = await RenderAsync(unit);

        html.Should().Contain("stat-tile stat-tile-flagged")
            .And.Contain(">OC<")
            .And.Contain("oc-battleshock-icon");
        // The numeric Oc value (1) is replaced, not shown alongside the glyph.
        html.Should().NotContain("<span class=\"stat-label\">OC</span><span class=\"stat-value\">1</span>");
    }

    [Fact]
    public async Task ClearingBattleShocked_RevertsTheOcTile()
    {
        var unit = AttachedUnitFixtures.LeaderUnit();
        unit.IsBattleShocked = true;
        (await RenderAsync(unit)).Should().Contain("oc-battleshock-icon");

        unit.IsBattleShocked = false;
        var html = await RenderAsync(unit);

        html.Should().NotContain("oc-battleshock-icon").And.NotContain("stat-tile-flagged");
    }

    [Fact]
    public async Task AttachedUnit_BattleShocked_EveryComponentsOcTileIsFlagged()
    {
        // Bodyguard (Crusader Squad) + Leader + Support each contribute their own statline run/OC
        // tile - Battle-shocked is one flag for the whole AttachedUnit, so all three must flip.
        var attachedUnit = AttachedUnitFixtures.DefaultAttachedUnit();
        attachedUnit.IsBattleShocked = true;

        var html = await RenderAsync(attachedUnit);

        var flaggedOcTileCount = Regex.Matches(html, "oc-battleshock-icon").Count;
        flaggedOcTileCount.Should().Be(3);
    }
}
