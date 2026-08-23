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

/// <summary>Exercises `_UnitBlock.cshtml`'s invulnerable-save rendering directly against hand-built
/// `AttachedUnitAggregateView` input, covering every `InvulnerableSave` shape - including
/// melee-only, ranged-only, and absent, none of which any real fixture in `Examples/` currently
/// exercises - rather than relying only on the example roster to happen to cover each case
/// (`redesign-invulnerable-save-display`). Renders through the real `IRazorPartialRenderer`/
/// `_UnitBlock.cshtml` path, the same one `/LivePlay` and the casualty endpoint both use, since the
/// branching under test lives entirely in the `.cshtml` - `LivePlayModel.BuildUnitBlock` itself
/// just carries the `Statline`/`InvulnerableSave` value through unchanged.</summary>
public class LivePlayInvulnerableSaveRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayInvulnerableSaveRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(InvulnerableSave insv)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: false,
            Statlines:
            [
                new AggregateStatlineEntry(
                    ComponentName: "Test Unit",
                    StatlineName: "Test Unit",
                    Statline: new Statline(6, 4, 3, 4, 7, 1) { InSv = insv },
                    RemainingCount: 1,
                    InitialCount: 1,
                    Loadouts: [])
            ],
            Weapons: [],
            Abilities: [],
            Keywords: new HashSet<string>());

        var unitBlock = LivePlayModel.BuildUnitBlock(view);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var model = new UnitBlockRenderModel(0, unitBlock, glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    [Fact]
    public async Task Absent_RendersNoInvulnerableSaveTile()
    {
        var html = await RenderAsync(new InvulnerableSave());

        html.Should().NotContain("insv-tile");
    }

    [Fact]
    public async Task Uniform_RendersBareLabelWithNoAttackTypeIndicator()
    {
        var html = await RenderAsync(new InvulnerableSave(4, 4, caveated: false, caveatAbility: null));

        html.Should().Contain(">InSv<").And.Contain(">4+<").And.NotContain("insv-icon").And.NotContain("insv-tile-caveated");
    }

    [Fact]
    public async Task MeleeOnly_RendersTheMeleeIconOnlyAndNoRangedIcon()
    {
        var html = await RenderAsync(new InvulnerableSave(4, 0, caveated: false, caveatAbility: null));

        html.Should().Contain(">InSv<").And.Contain("⚔ 4+").And.NotContain("insv-icon");
    }

    [Fact]
    public async Task RangedOnly_RendersTheRangedIconOnlyAndNoMeleeIcon()
    {
        var html = await RenderAsync(new InvulnerableSave(0, 5, caveated: false, caveatAbility: null));

        html.Should().Contain(">InSv<").And.Contain("insv-icon").And.Contain("5+").And.NotContain("⚔");
    }

    [Fact]
    public async Task DifferingBothKnown_RendersBothValuesAndBothIcons()
    {
        var html = await RenderAsync(new InvulnerableSave(4, 5, caveated: false, caveatAbility: null));

        html.Should().Contain("stat-value-compact")
            .And.Contain("⚔ 4+")
            .And.Contain("5+")
            .And.Contain("insv-icon")
            .And.NotContain("+ / ");
    }

    [Fact]
    public async Task Caveated_RendersOffColorTileBareLabelAndTheAbilityText()
    {
        var ability = new Ability
        {
            Name = "Test Ability",
            Text = "This model has a test invulnerable save condition.",
            Scope = AbilityScope.Unit,
            Origin = AbilityOrigin.Intrinsic
        };
        var html = await RenderAsync(new InvulnerableSave(5, 5, caveated: true, caveatAbility: ability));

        html.Should().Contain("insv-tile-caveated")
            .And.Contain(">InSv*<")
            .And.Contain("This model has a test invulnerable save condition.")
            .And.NotContain("insv-icon");
    }
}
