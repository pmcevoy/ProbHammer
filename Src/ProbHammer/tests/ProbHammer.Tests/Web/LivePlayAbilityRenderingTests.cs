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

/// <summary>Exercises `_UnitBlock.cshtml`'s ability-name rendering directly against a hand-built
/// `AttachedUnitAggregateView` - specifically the `display-resolved-enhancements` visual indicator
/// (a leading "✦ " on an Enhancement-classified ability's rendered name), which lives entirely in
/// the `.cshtml`'s `AbilityDisplayName` helper. Same real-Razor-path harness as
/// `LivePlayInvulnerableSaveRenderingTests`.</summary>
public class LivePlayAbilityRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayAbilityRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(IReadOnlyList<AggregateAbilityEntry> abilities)
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
                    Statline: new Statline(6, 4, 3, 4, 7, 1),
                    RemainingCount: 1,
                    InitialCount: 1,
                    Loadouts: [])
            ],
            Weapons: [],
            Abilities: abilities,
            Keywords: new HashSet<string>());

        var unitBlock = LivePlayModel.BuildUnitBlock(view);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var model = new UnitBlockRenderModel(0, unitBlock, glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    [Fact]
    public async Task EnhancementClassifiedAbility_RendersWithTheStarPrefix()
    {
        var enhancement = new Ability
        {
            Name = "Oathbound Exemplar", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Enhancement
        };
        var html = await RenderAsync([new AggregateAbilityEntry("Test Unit", StatlineName: null, enhancement)]);

        html.Should().Contain("✦ Oathbound Exemplar");
    }

    [Fact]
    public async Task NonEnhancementAbility_RendersWithNoPrefix()
    {
        var intrinsic = new Ability
        {
            Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };
        var html = await RenderAsync([new AggregateAbilityEntry("Test Unit", StatlineName: null, intrinsic)]);

        html.Should().Contain("Righteous Zeal").And.NotContain("✦");
    }
}
