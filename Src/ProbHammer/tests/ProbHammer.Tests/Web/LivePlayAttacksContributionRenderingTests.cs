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

public class LivePlayAttacksContributionRenderingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LivePlayAttacksContributionRenderingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<IReadOnlyList<string>> RenderBreakdownRowClassesAsync(AggregateWeaponEntry weaponEntry)
    {
        using var scope = _factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var statlines = weaponEntry.Contributions
            .Select(c => new AggregateStatlineEntry(
                ComponentName: c.ComponentName,
                StatlineName: c.StatlineName,
                Statline: new Statline(6, 4, 3, 4, 7, 1),
                RemainingCount: c.Count,
                InitialCount: c.Count,
                Loadouts: []))
            .ToList();

        var view = new AttachedUnitAggregateView(
            Name: "Squad A",
            IsAttachedUnit: false,
            Statlines: statlines,
            Weapons: [weaponEntry],
            Abilities: [],
            Keywords: new HashSet<string>());

        var model = new UnitBlockRenderModel(0, LivePlayModel.BuildUnitBlock(view),
            RuleGlossary.Build(new BsdataClosure([])));
        var html = await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);

        return Regex.Matches(html, "<tr class=\"([^\"]*)\"[^>]*data-weapon-id=")
            .Select(m => m.Groups[1].Value)
            .Where(c => c.Contains("contribution-row"))
            .ToList();
    }

    private static Ability TestAbility(string name) =>
        new() { Name = name, Text = "...", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic };

    [Fact]
    public async Task GroupWideAttacksLine_RendersAfterEveryBaseContributorRow()
    {
        var ability = TestAbility("Aura Buff");
        var entry = new AggregateWeaponEntry(
            Profile: new MeleeWeapon("Test Blade", DiceExpression.Fixed(1), 3, 4, 0, 1),
            TotalAttacks: DiceExpression.Fixed(10),
            Name: "Test Blade",
            Contributions:
            [
                new WeaponContribution("Squad A", "Trooper", 3, DiceExpression.Fixed(1), "Test Blade",
                    AttacksContributions: [new AttacksContribution(ability, 1)]),
                new WeaponContribution("Squad A", "Gunner", 1, DiceExpression.Fixed(3), "Test Blade",
                    AttacksContributions: [new AttacksContribution(ability, 1)])
            ]);

        var rows = await RenderBreakdownRowClassesAsync(entry);

        rows.Should().HaveCount(3);
        rows.Take(2).Should().OnlyContain(c => c.StartsWith("weapon-contribution-row"));
        rows.Last().Should().Contain("weapon-attacks-contribution-row").And.Contain("group-wide");
    }

    [Fact]
    public async Task RowBoundAttacksLine_RendersDirectlyUnderTheBaseRowItReaches()
    {
        var ability = TestAbility("Squad Boost");
        var entry = new AggregateWeaponEntry(
            Profile: new MeleeWeapon("Test Blade", DiceExpression.Fixed(1), 3, 4, 0, 1),
            TotalAttacks: DiceExpression.Fixed(8),
            Name: "Test Blade",
            Contributions:
            [
                new WeaponContribution("Squad A", "Trooper", 3, DiceExpression.Fixed(1), "Test Blade",
                    AttacksContributions: [new AttacksContribution(ability, 1)]),
                new WeaponContribution("Squad A", "Sarge", 1, DiceExpression.Fixed(2), "Test Blade")
            ]);

        var rows = await RenderBreakdownRowClassesAsync(entry);

        var rowBoundIndex = rows.ToList().FindIndex(c => c.Contains("row-bound"));
        rowBoundIndex.Should().BePositive();
        rows[rowBoundIndex - 1].Should().StartWith("weapon-contribution-row");
        rows.Should().NotContain(c => c.Contains("group-wide"));
    }
}
