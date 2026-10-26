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

public class LivePlayFeelNoPainRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayFeelNoPainRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static Ability Ability(string name) => new()
    {
        Name = name, Text = $"{name} test text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
    };

    private static readonly Ability OwnFivePlus = Ability("Feel No Pain 5+");

    private static ScalarCharacteristicView Fnp(int threshold, Ability source) =>
        ScalarCharacteristicView.Resolved(Statline.NoFeelNoPain.OriginalValue, threshold, [source]);

    private static AggregateStatlineEntry Entry(string name, ScalarCharacteristicView? fnp = null,
        params NotAppliedStatlineEffect[] notApplied) =>
        new("Test Unit", name, new Statline(6, 4, 3, 4, 7, 1) { Fnp = fnp ?? Statline.NoFeelNoPain }, 1, 1, [],
            NotAppliedEffects: notApplied);

    private Task<string> RenderAsync(params AggregateStatlineEntry[] entries) => RenderAsync(entries, []);

    private async Task<string> RenderAsync(IReadOnlyList<AggregateStatlineEntry> entries,
        IReadOnlyList<Ability> datasheetAbilities)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var view = new AttachedUnitAggregateView("Test Unit", false, entries, [],
            datasheetAbilities.Select(a => new AggregateAbilityEntry("Test Unit", null, a)).ToList(),
            new HashSet<string>());
        var model = new UnitBlockRenderModel(0, LivePlayModel.BuildUnitBlock(view),
            RuleGlossary.Build(new BsdataClosure([])));
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    private static readonly EffectCondition OncePerBattle = new(UsageLimit.OncePerBattle, null, null, false);

    [Fact]
    public async Task AnOwnFeelNoPain_RendersAnAmberTileWhosePopoverNamesItsSource()
    {
        var html = await RenderAsync(Entry("Poxwalker", Fnp(5, OwnFivePlus)));

        html.Should().Contain("class=\"stat-tile fnp-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">FNP</span><span class=\"stat-value\">5+</span>")
            .And.Contain(">Feel No Pain 5+<")
            .And.Contain("<span data-prov-original>–</span>")
            .And.Contain("<span data-prov-total>5+</span>")
            .And.NotContain("provenance-cond");
    }

    [Fact]
    public async Task NoFeelNoPainAndNothingReachingIt_RendersNoTile()
    {
        var html = await RenderAsync(Entry("Poxwalker"));

        html.Should().NotContain("fnp-tile");
    }

    [Fact]
    public async Task RunsDifferingOnlyInFeelNoPain_RenderSeparateStatTiles()
    {
        var html = await RenderAsync(Entry("Champion", Fnp(4, Ability("Feel No Pain 4+"))), Entry("Trooper"));

        html.Split("class=\"statline-tiles\"").Length.Should().Be(3);
    }

    [Fact]
    public async Task AFootnotedFeelNoPain_RendersAnAmberTileWithACaveatLine()
    {
        var starred = Ability("Feel No Pain 3+*");
        var html = await RenderAsync(Entry("Warden", ScalarCharacteristicView.Caveated(3, starred)));

        html.Should().Contain("class=\"stat-tile fnp-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">FNP</span><span class=\"stat-value\">3+</span>")
            .And.Contain("<th>Datasheet</th>")
            .And.Contain("May modify this value; check its text.")
            .And.NotContain("provenance-result");
    }

    [Fact]
    public async Task AOncePerBattleFeelNoPainOnAUnitWithoutOne_RendersAConditionalDashTile()
    {
        var html = await RenderAsync(Entry("Warden", null,
            new NotAppliedStatlineEffect(Ability("Living Fortress"), new FeelNoPainEffect(4), OncePerBattle)));

        html.Should().Contain("class=\"stat-tile fnp-tile provenance-tile provenance-cond\"")
            .And.Contain("<span class=\"stat-label\">FNP</span><span class=\"stat-value\">–</span>")
            .And.Contain(">Living Fortress<")
            .And.Contain("<td>4+</td>")
            .And.Contain("Once per battle; not added");
    }

    [Fact]
    public async Task AnInForceQualifiedFeelNoPain_IsACaveatLine_AndLeavesTheValue()
    {
        var html = await RenderAsync(Entry("Poxwalker", Fnp(6, Ability("Feel No Pain 6+")),
            new NotAppliedStatlineEffect(Ability("Warding"), new FeelNoPainEffect(5, "mortal wounds"),
                new EffectCondition(null, null, null, false), IsInForce: true)));

        html.Should().Contain("class=\"stat-tile fnp-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">FNP</span><span class=\"stat-value\">6+</span>")
            .And.Contain(">Warding<")
            .And.Contain("<td colspan=\"2\">mortal wounds</td>")
            .And.Contain("Not included in the value.")
            .And.Contain("<span data-prov-total>6+</span>");
    }

    [Fact]
    public async Task AnInForceQualifiedFeelNoPainOnAUnitWithoutOne_RendersAnAmberDashTile()
    {
        var html = await RenderAsync(Entry("Captain", null,
            new NotAppliedStatlineEffect(Ability("Iron Will"), new FeelNoPainEffect(4, "CHARACTER models only"),
                new EffectCondition(null, null, null, false), IsInForce: true)));

        html.Should().Contain("class=\"stat-tile fnp-tile provenance-tile\"")
            .And.Contain("<span class=\"stat-label\">FNP</span><span class=\"stat-value\">–</span>")
            .And.Contain("CHARACTER models only")
            .And.NotContain("provenance-cond");
    }

    [Fact]
    public async Task AConditionalQualifiedFeelNoPain_IsANotAddedLineWithItsConditionAndQualifier()
    {
        var html = await RenderAsync(Entry("Warden", null,
            new NotAppliedStatlineEffect(Ability("Warding"), new FeelNoPainEffect(4, "Psychic Attacks"),
                OncePerBattle)));

        html.Should().Contain("provenance-cond")
            .And.Contain("Once per battle; not added")
            .And.Contain("<td colspan=\"2\">Psychic Attacks</td>")
            .And.NotContain("Not included in the value.");
    }

    private static readonly Ability ValuelessFeelNoPain = Ability("Feel No Pain");

    [Fact]
    public async Task AValuelessFeelNoPainEntry_IsHiddenBesideAnFnpTile()
    {
        var fortress = Ability("Living Fortress");
        var html = await RenderAsync(
            [Entry("Warden", null, new NotAppliedStatlineEffect(fortress, new FeelNoPainEffect(4), OncePerBattle))],
            [ValuelessFeelNoPain, fortress]);

        html.Should().Contain("fnp-tile")
            .And.Contain(">Living Fortress<")
            .And.NotContain(">Feel No Pain<");
    }

    [Fact]
    public async Task AValuelessFeelNoPainEntry_WithNoFnpTile_StaysListed()
    {
        var html = await RenderAsync([Entry("Warden")], [ValuelessFeelNoPain]);

        html.Should().NotContain("fnp-tile").And.Contain(">Feel No Pain<");
    }

    [Fact]
    public async Task AValuedFeelNoPainEntry_StaysListedBesideItsTile()
    {
        var html = await RenderAsync([Entry("Poxwalker", Fnp(5, OwnFivePlus))], [OwnFivePlus]);

        var abilityCell = html[html.IndexOf("spans-component", StringComparison.Ordinal)..];
        html.Should().Contain("fnp-tile");
        abilityCell.Should().Contain(">Feel No Pain 5+<");
    }
}
