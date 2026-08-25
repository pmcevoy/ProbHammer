using System.Text.RegularExpressions;
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

/// <summary>Exercises `_ArmyHeader.cshtml` (display-army-header-and-detachment-rules) against
/// hand-built `ArmyRoster`s - roster metadata, Detachment rule rendering (zero/one/many rules per
/// Detachment), the roster-wide `ArmyRule` column (including the two-simultaneous-abilities case
/// design.md flags as architecturally-sound-but-unexercised, since no real captured export in the
/// repo is Drukhari), and that the existing per-unit `ArmyRule` box is unaffected. Same real-Razor-
/// path harness as `LivePlayAbilityRenderingTests`.</summary>
public class LivePlayArmyHeaderRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayArmyHeaderRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(string partialPath, object model)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();
        return await renderer.RenderAsync(httpContext, partialPath, model);
    }

    private async Task<string> RenderHeaderAsync(ArmyRoster roster)
    {
        var header = LivePlayModel.BuildArmyHeader(roster);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        return await RenderAsync("/Pages/Shared/_ArmyHeader.cshtml", new ArmyHeaderRenderModel(header, glossary));
    }

    private static ArmyRoster Roster(IEnumerable<ICombatUnit> units, IEnumerable<ResolvedDetachment> detachments) =>
        new(
            name: "Sons of the Emperor",
            pointsSpent: 750,
            faction: ["Test Codex", "Test Sub-Faction"],
            detachments: detachments,
            forceDisposition: "Bringers of Fire",
            battleSize: "Incursion",
            pointsLimit: 1000,
            units: units);

    private static Unit UnitWithAbility(string name, Ability ability)
    {
        var statline = new Statline(6, 4, 3, 2, 6, 1);
        var datasheet = new Datasheet(
            name: name,
            factionKeywords: [],
            keywords: [],
            abilities: [ability],
            statlines: [(name, statline)],
            weaponProfiles: []);

        return new Unit(datasheet, enhancements: [], modelLines: [new ModelLine(name, [], count: 1)]);
    }

    [Fact]
    public async Task HeaderRendersRosterMetadata()
    {
        var roster = Roster([], [new ResolvedDetachment("Test Detachment", [])]);

        var html = await RenderHeaderAsync(roster);

        html.Should().Contain("Sons of the Emperor");
        html.Should().Contain("Test Codex - Test Sub-Faction");
        html.Should().Contain("Incursion");
        html.Should().Contain("750").And.Contain("1000");
        html.Should().Contain("Bringers of Fire");
    }

    [Fact]
    public async Task ADetachmentWithOneRule_ShowsOnePopoverTriggerBeneathItsName()
    {
        var detachment = new ResolvedDetachment("Marshal's Household",
            [new DetachmentRule("Faith-Fuelled Resolve", "Full text.")]);
        var roster = Roster([], [detachment]);

        var html = await RenderHeaderAsync(roster);

        // Razor HTML-encodes the apostrophe in "Marshal's Household" (@detachment.Name is a plain
        // encoded expression, not @Html.Raw) - matched loosely rather than asserting the exact
        // entity form.
        html.Should().MatchRegex("Marshal.{1,6}s Household");
        html.Should().Contain("Faith-Fuelled Resolve");
        Regex.Matches(html, "detachment-name").Count.Should().Be(1);
    }

    [Fact]
    public async Task ADetachmentWithMoreThanOneRule_ShowsOneTriggerPerRule()
    {
        var detachment = new ResolvedDetachment("Black Spear Task Force",
            [new DetachmentRule("Kill Teams", "..."), new DetachmentRule("Mission Tactics", "...")]);
        var roster = Roster([], [detachment]);

        var html = await RenderHeaderAsync(roster);

        html.Should().Contain("Black Spear Task Force");
        html.Should().Contain("Kill Teams");
        html.Should().Contain("Mission Tactics");
    }

    [Fact]
    public async Task AZeroRuleDetachment_ShowsItsNameWithNoPopoverTrigger()
    {
        var detachment = new ResolvedDetachment("Warhost", []);
        var roster = Roster([], [detachment]);

        var html = await RenderHeaderAsync(roster);

        html.Should().Contain("Warhost");
        html.Should().NotContain("ability-name-line");
    }

    [Fact]
    public async Task TwoSimultaneousArmyRuleAbilities_BothRenderIndependentlyInTheArmyColumn()
    {
        // design.md's Risk: no real captured export in the repo is Drukhari/Aeldari, the two
        // confirmed real cases of two simultaneous ArmyRule-origin abilities - hand-built here.
        var powerFromPain = new Ability
            { Name = "Power from Pain", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.ArmyRule };
        var corsairs = new Ability
        {
            Name = "Corsairs and Travelling Players", Text = "...", Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.ArmyRule
        };
        var roster = Roster(
            [UnitWithAbility("Kabalite Warriors", powerFromPain), UnitWithAbility("Wracks", corsairs)],
            [new ResolvedDetachment("Test Detachment", [])]);

        var html = await RenderHeaderAsync(roster);

        html.Should().Contain(">Army<");
        html.Should().Contain("Power from Pain");
        html.Should().Contain("Corsairs and Travelling Players");
    }

    [Fact]
    public async Task NoArmyRuleAbility_OmitsTheArmyColumnEntirely()
    {
        var roster = Roster([], [new ResolvedDetachment("Test Detachment", [])]);

        var html = await RenderHeaderAsync(roster);

        html.Should().NotContain(">Army<");
    }

    [Fact]
    public async Task ArmyRuleAbility_RendersBothInTheHeaderAndOnItsOwnUnit()
    {
        var vow = new Ability
            { Name = "Templar Vows", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.ArmyRule };
        var unit = UnitWithAbility("Crusader Squad", vow);
        var roster = Roster([unit], [new ResolvedDetachment("Test Detachment", [])]);

        var headerHtml = await RenderHeaderAsync(roster);
        headerHtml.Should().Contain("Templar Vows");

        var unitBlocks = LivePlayModel.BuildUnitBlocks(roster);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var unitHtml = await RenderAsync("/Pages/Shared/_UnitBlock.cshtml",
            new UnitBlockRenderModel(0, unitBlocks[0], glossary));
        unitHtml.Should().Contain("Templar Vows");
    }

    [Fact]
    public async Task NoDpCostAndNoUnitPointsCostRenderAnywhereInTheHeader()
    {
        var detachment = new ResolvedDetachment("Test Detachment", [new DetachmentRule("Some Rule", "...")]);
        var roster = Roster([], [detachment]);

        var html = await RenderHeaderAsync(roster);

        html.Should().NotContain("Detachment Points").And.NotContain("DP");
    }
}