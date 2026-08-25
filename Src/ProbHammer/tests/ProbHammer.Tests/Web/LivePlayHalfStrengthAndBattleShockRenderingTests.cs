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

/// <summary>Exercises `_UnitBlock.cshtml`'s `.unit-toolbar` Half Strength/Battle-shock controls
/// (half-strength-and-battleshock-indicators; relocated from the unit-name header into the toolbar
/// by consolidate-unit-toolbar) and the Battle-shocked Objective Control tile rendering, against
/// real `ICombatUnit` fixtures rather than a bare `AttachedUnitAggregateView` - unlike
/// `LivePlayInvulnerableSaveRenderingTests`, the branching under test here (single-model vs.
/// multi-model control shape, Battle-shocked OC rendering) reads `ICombatUnit`-level state
/// (`HalfStrengthResolution`, `IsBattleShocked`) that only exists via
/// `LivePlayModel.BuildUnitBlock(view, unit)`'s two-argument overload.</summary>
public class LivePlayHalfStrengthAndBattleShockRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayHalfStrengthAndBattleShockRenderingTests(WebApplicationFactory<Program> factory) =>
        _factory = factory;

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

    // Matches a rendered <button>'s own opening tag by a class it carries - across whatever
    // whitespace/line breaks the Razor source (or any downstream reformatting of it) puts between
    // attributes, since attribute order/line breaks are not a contract these tests should depend
    // on; only which attributes are present matters.
    private static string ButtonTag(string html, string glyphClass) =>
        Regex.Match(html, $@"<button[^>]*{Regex.Escape(glyphClass)}[^>]*>").Value;

    [Fact]
    public async Task SingleModelUnit_HalfStrengthControlIsAlwaysActionable_ReflectingTheOverride()
    {
        var unit = AttachedUnitFixtures.LeaderUnit();

        var pristineHtml = await RenderAsync(unit);
        // The single-model branch's own aria-label - unique to it, unlike the multi-model branch's
        // "(computed)" wording - and it never carries a `disabled` attribute.
        pristineHtml.Should().Contain("aria-label=\"Toggle at-or-below half strength\"");
        pristineHtml.Should().NotContain("aria-label=\"At or below half strength (computed)\"");
        pristineHtml.Should().NotContain("status-glyph-halfstrength is-active");

        unit.IsHalfStrengthOverride = true;
        var setHtml = await RenderAsync(unit);
        setHtml.Should().Contain("status-glyph-halfstrength is-active");
    }

    [Fact]
    public async Task MultiModelUnit_AboveHalfStrength_RendersTheHalfStrengthControlInertAndInactive()
    {
        // Every toolbar control always renders now (consolidate-unit-toolbar) - the multi-model
        // case is a permanently-`disabled` <button>, never a listener-less <span>, so both the
        // "computed, not a player toggle" aria-label and the `disabled` attribute are always
        // present regardless of the computed status; only the glyph's active/inactive color
        // (the "is-active" class) reflects that status.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader(); // 5 full-health models

        var html = await RenderAsync(unit);

        html.Should().Contain("aria-label=\"At or below half strength (computed)\"");
        var button = ButtonTag(html, "status-glyph-halfstrength");
        button.Should().Contain("disabled");
        button.Should().NotContain("is-active");
    }

    [Fact]
    public async Task MultiModelUnit_AtOrBelowHalfStrength_RendersTheHalfStrengthControlInertButActive()
    {
        // Sergeant (1) + troopers (4) = 5; half = floor(5/2) = 2.
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();
        var troopers = unit.ModelLines.Single(ml => ml.StatlineName == "Assault Intercessor");
        troopers.SetRemainingCount(1); // sergeant (1) + troopers (1) = 2 <= 2

        var html = await RenderAsync(unit);

        // Still inert (disabled) - only the glyph color changes to reflect the computed status.
        var button = ButtonTag(html, "status-glyph-halfstrength");
        button.Should().Contain("is-active");
        button.Should().Contain("disabled");
    }

    [Fact]
    public async Task TheBattleShockControl_AlwaysRendersActionable_RightmostOfTheStatusControls()
    {
        var unit = UnitFixtures.AssaultIntercessorSquadWithUnitLeader();

        var html = await RenderAsync(unit);

        html.Should().Contain("status-glyph-battleshock");
        // Battle-shock is always actionable, for every unit regardless of starting strength -
        // unlike Half Strength, it never renders `disabled`.
        ButtonTag(html, "status-glyph-battleshock").Should().NotContain("disabled");
    }

    [Fact]
    public async Task BothControlsPresent_BattleShockRendersAfterHalfStrengthInMarkupOrder()
    {
        var unit = AttachedUnitFixtures.LeaderUnit(); // single-model - Half Strength is always actionable
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