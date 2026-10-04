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

public class LivePlayKeywordChipRenderingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string LethalHitsText = "Each time an attack is made with this weapon, a Critical Hit automatically wounds.";

    private static readonly RuleGlossary Glossary = RuleGlossary.BuildFrom(
        [new RuleDefinition("Lethal Hits", [], LethalHitsText, [])]);

    private static readonly Ability DetachmentRule = new()
    {
        Name = "Rapid Assault", Text = "Rapid Assault rules text.", Scope = AbilityScope.Unit,
        Origin = AbilityOrigin.DetachmentRule
    };

    private async Task<string> RenderAsync(IReadOnlyList<string> keywords, IReadOnlyList<KeywordGrant>? grants = null,
        IReadOnlyList<NotAppliedKeywordGrant>? notAdded = null)
    {
        using var scope = factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();

        var weapon = new MeleeWeapon("Power sword", A: 3, Ws: 3, S: 4, Ap: -2, D: 1) { KeywordsText = keywords };
        var entry = new AggregateWeaponEntry(weapon, DiceExpression.Fixed(3), weapon.Name,
            [new WeaponContribution("Test Unit", "Trooper", 1, DiceExpression.Fixed(3), weapon.Name)],
            KeywordGrants: grants, NotAppliedKeywordGrants: notAdded);
        var view = new AttachedUnitAggregateView("Test Unit", false,
            [new AggregateStatlineEntry("Test Unit", "Trooper", new Statline(6, 4, 3, 2, 6, 1), 1, 1, [])],
            [entry], [], new HashSet<string>());

        var model = new UnitBlockRenderModel(0, LivePlayModel.BuildUnitBlock(view), Glossary);
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    private static string PanelFor(string html, string triggerClass)
    {
        var target = Regex.Match(html, $"class=\"{triggerClass}\" popovertarget=\"(p-[^\"]+)\"").Groups[1].Value;
        target.Should().NotBeEmpty();
        var start = html.IndexOf($"<div id=\"{target}\"", StringComparison.Ordinal);
        var next = html.IndexOf("<div id=\"p-", start + 1, StringComparison.Ordinal);
        return next < 0 ? html[start..] : html[start..next];
    }

    [Fact]
    public async Task AnAppliedGrantsPopover_NamesItsSourceAboveTheKeywordsRuleText()
    {
        var html = await RenderAsync(["Lethal Hits"], grants: [new KeywordGrant(DetachmentRule, "Lethal Hits", null)]);

        html.Should().Contain("class=\"weapon-tag weapon-tag-granted\"");
        var panel = PanelFor(html, "weapon-tag weapon-tag-granted");
        panel.Should().Contain("class=\"ability-name-line\"").And.Contain("Rapid Assault").And.Contain(LethalHitsText);
        panel.IndexOf("Rapid Assault", StringComparison.Ordinal)
            .Should().BeLessThan(panel.IndexOf(LethalHitsText, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AReplacingGrantsPopover_SaysWhatItReplaces()
    {
        var html = await RenderAsync(["Sustained Hits 2"],
            grants: [new KeywordGrant(DetachmentRule, "Sustained Hits 2", "Sustained Hits 1")]);

        PanelFor(html, "weapon-tag weapon-tag-granted").Should().Contain("replaces Sustained Hits 1");
        html.Should().NotContain(">Sustained Hits 1</span>");
    }

    [Fact]
    public async Task ANotAddedGrantsPopover_GivesItsConditionAndThatItWasNotAdded()
    {
        var html = await RenderAsync(["Assault"], notAdded:
            [new NotAppliedKeywordGrant(DetachmentRule, "Lance", new EffectCondition(null, null, "If this unit charged", false))]);

        var panel = PanelFor(html, "weapon-tag weapon-tag-cond");
        panel.Should().Contain("Rapid Assault").And.Contain("If this unit charged; not added")
            .And.Contain("provenance-not-applied");
    }

    [Fact]
    public async Task AGrantedKeywordWithNoGlossaryEntry_IsStillATrigger()
    {
        var html = await RenderAsync(["Assault"], notAdded:
            [new NotAppliedKeywordGrant(DetachmentRule, "Lance", new EffectCondition(null, null, null, false))]);

        html.Should().MatchRegex("<button type=\"button\" id=\"[^\"]+\" class=\"weapon-tag weapon-tag-cond\" popovertarget=");
        html.Should().Contain("<span class=\"weapon-tag weapon-tag-unresolved\">Assault</span>");
    }

    [Fact]
    public async Task AWeaponsOwnChipPopover_IsUnchanged()
    {
        var html = await RenderAsync(["Lethal Hits"]);

        var panel = PanelFor(html, "weapon-tag weapon-tag-resolved");
        panel.Should().Contain(LethalHitsText).And.NotContain("provenance-table");
        html.Should().NotContain("weapon-tag-granted").And.NotContain("weapon-tag-cond");
    }
}
