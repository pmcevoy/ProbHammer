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

/// <summary>Exercises `_UnitBlock.cshtml`'s weapon-name rendering directly against a hand-built
/// `AttachedUnitAggregateView` input, confirming a merged entry's composite `Name` (see
/// `name-weapon-group-contributions`) renders in the weapon-name cell and as the
/// breakdown-toggle trigger text, rather than an arbitrary single contributor's own
/// `Profile.Name`. Renders through the real `IRazorPartialRenderer`/`_UnitBlock.cshtml` path, the
/// same one `/LivePlay` and the casualty endpoint both use.</summary>
public class LivePlayWeaponNameRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayWeaponNameRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> RenderAsync(AggregateWeaponEntry weaponEntry, int statlineCount)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var statlines = Enumerable.Range(0, statlineCount)
            .Select(i => new AggregateStatlineEntry(
                ComponentName: "Test Unit",
                StatlineName: $"Model {i}",
                Statline: new Statline(6, 4, 3, 4, 7, 1),
                RemainingCount: 1,
                InitialCount: 1,
                Loadouts: []))
            .ToList();

        var view = new AttachedUnitAggregateView(
            Name: "Test Unit",
            IsAttachedUnit: false,
            Statlines: statlines,
            Weapons: [weaponEntry],
            Abilities: [],
            Keywords: new HashSet<string>());

        var unitBlock = LivePlayModel.BuildUnitBlock(view);
        var glossary = RuleGlossary.Build(new BsdataClosure([]));
        var model = new UnitBlockRenderModel(0, unitBlock, glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    [Fact]
    public async Task MergedEntry_RendersItsCompositeNameInTheWeaponNameCell()
    {
        var profile = new RangedWeapon("Bolt rifle", 24, 1, 3, 4, -1, 1);
        var entry = new AggregateWeaponEntry(
            Profile: profile,
            TotalAttacks: DiceExpression.Fixed(2),
            Name: "Bolt rifle and Combat rifle",
            Contributions:
            [
                new WeaponContribution("Test Unit", "Model 0", 1, DiceExpression.Fixed(1), "Bolt rifle"),
                new WeaponContribution("Test Unit", "Model 1", 1, DiceExpression.Fixed(1), "Combat rifle")
            ]);

        var html = await RenderAsync(entry, statlineCount: 2);

        html.Should().Contain("Bolt rifle and Combat rifle").And.NotContain(">Bolt rifle<").And.NotContain(">Combat rifle<");
    }

    [Fact]
    public async Task MergedEntry_RendersItsCompositeNameAsTheBreakdownToggleTrigger()
    {
        var profile = new RangedWeapon("Bolt rifle", 24, 1, 3, 4, -1, 1);
        var entry = new AggregateWeaponEntry(
            Profile: profile,
            TotalAttacks: DiceExpression.Fixed(2),
            Name: "Bolt rifle and Combat rifle",
            Contributions:
            [
                new WeaponContribution("Test Unit", "Model 0", 1, DiceExpression.Fixed(1), "Bolt rifle"),
                new WeaponContribution("Test Unit", "Model 1", 1, DiceExpression.Fixed(1), "Combat rifle")
            ]);

        // Two statlines (two ModelLines total) means every weapon entry renders its name as an
        // interactive breakdown-toggle trigger - see live-play-view's "Weapon Section Rendering".
        var html = await RenderAsync(entry, statlineCount: 2);

        html.Should().Contain("weapon-name-toggle").And.Contain("Bolt rifle and Combat rifle");
    }
}
