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

    [Fact]
    public async Task AComponentLessAbility_RendersOnceInItsOwnRowAboveEveryComponent()
    {
        // gate-and-dedupe-core-rule-abilities: a deduplicated Core Rule ability (ComponentName
        // null) renders as its own grid row (row 1), not aligned to or spanning any component's
        // own rows.
        var vow = new Ability { Name = "Templar Vows", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.CoreRule };
        var html = await RenderAsync([new AggregateAbilityEntry(
            ComponentName: null, StatlineName: null, Ability: vow, ContributingComponentNames: ["Test Unit"])]);

        html.Should().Contain("spans-whole-unit");
        html.Should().Contain("Templar Vows");
        html.Should().Contain("grid-row: 1;");
    }

    [Fact]
    public async Task AComponentLessAbility_ShiftsEveryOtherRowDownByOne()
    {
        var vow = new Ability { Name = "Templar Vows", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.CoreRule };
        var html = await RenderAsync([new AggregateAbilityEntry(
            ComponentName: null, StatlineName: null, Ability: vow, ContributingComponentNames: ["Test Unit"])]);

        // The single statline row would normally render at grid-row: 1 with no whole-unit span
        // present; with one present, it must shift to grid-row: 2 to make room for it.
        html.Should().Contain("grid-row: 2;");
    }

    [Fact]
    public async Task ARowBoundAbility_AbsorbsIntoASingleRowComponentWideSpan_InsteadOfColliding()
    {
        // Real-corpus regression: Impulsor's row-bound "Shield Dome" (its only statline row) was
        // invisible behind its own component-wide "Transport"/"Assault Vehicle" cell - both cells
        // occupied the identical grid coordinates, the later one in DOM order painting over the
        // first. Both must now render, and neither cell should render twice.
        var shieldDome = new Ability { Name = "Shield Dome", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.OptionalGrant };
        var transport = new Ability { Name = "Transport", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };
        var html = await RenderAsync(
        [
            new AggregateAbilityEntry("Test Unit", StatlineName: "Test Unit", shieldDome),
            new AggregateAbilityEntry("Test Unit", StatlineName: null, transport)
        ]);

        html.Should().Contain("Shield Dome");
        html.Should().Contain("Transport");
        // Both abilities end up in the one merged span cell, not a separate row-bound cell too.
        var unitAbilityCellCount = System.Text.RegularExpressions.Regex.Matches(html, "col-unit-abilities").Count;
        unitAbilityCellCount.Should().Be(1);
    }

    [Fact]
    public async Task WithNoComponentLessAbility_RowsAreNotShifted()
    {
        var intrinsic = new Ability { Name = "Righteous Zeal", Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };
        var html = await RenderAsync([new AggregateAbilityEntry("Test Unit", StatlineName: null, intrinsic)]);

        html.Should().NotContain("spans-whole-unit");
        html.Should().Contain("grid-row: 1;").And.NotContain("grid-row: 2;");
    }
}
