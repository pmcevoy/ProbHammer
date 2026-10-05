using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Services;

namespace ProbHammer.Tests.Web;

public class LivePlayConditionControlRenderingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly MeleeWeapon Hammer = new("Daemon hammer", A: 4, Ws: 2, S: 8, Ap: -2, D: 3);

    private static readonly Ability Glory = new()
    {
        Name = "Chance for Glory", Text = "Chance for Glory rules text.", Scope = AbilityScope.Model,
        Origin = AbilityOrigin.Intrinsic
    };

    private static readonly Ability DarkPacts = new()
    {
        Name = "Dark Pacts", Text = "Dark Pacts rules text.", Scope = AbilityScope.Unit,
        Origin = AbilityOrigin.Intrinsic
    };

    private static readonly Ability Frenzy = new()
    {
        Name = "Frenzy", Text = "Frenzy rules text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
    };

    private static ClassifiedEffect Branch(string keyword, int group, int option) => new()
    {
        Effect = new WeaponKeywordGrantEffect(new AllWeapons(), keyword),
        ResidualConditionBucket = ResidualConditionBucket.None,
        ChoiceBranch = new ChoiceBranch(group, option)
    };

    private static readonly AbilityClassificationCatalogue Catalogue = ClassificationFixtures.Catalogue(
    [
        (Glory.Text, new AbilityClassification
        {
            Target = new SelfRuleTarget(),
            Effects =
            [
                new ClassifiedEffect
                {
                    Effect = new WeaponCharacteristicEffect(new WeaponClass(WeaponType.Melee), "S", EffectVerb.Improve,
                        1),
                    ResidualConditionBucket = ResidualConditionBucket.None
                }
            ],
            UsageLimit = UsageLimit.OncePerBattle,
            CoverageStatus = CoverageStatus.Complete
        }),
        (DarkPacts.Text, new AbilityClassification
        {
            Target = new AttachedUnitRuleTarget(),
            Effects = [Branch("Lethal Hits", 0, 0), Branch("Sustained Hits 1", 0, 1)],
            ChoiceGroups = [new ChoiceGroup(1, 1, ["[LETHAL HITS]", "[SUSTAINED HITS 1]"])],
            CoverageStatus = CoverageStatus.Complete
        }),
        (Frenzy.Text, new AbilityClassification
        {
            Target = new AttachedUnitRuleTarget(),
            Effects = [Branch("Lance", 0, 0), Branch("Assault", 0, 1), Branch("Precision", 0, 2)],
            ChoiceGroups = [new ChoiceGroup(1, 2, ["[LANCE]", "[ASSAULT]", "[PRECISION]"])],
            CoverageStatus = CoverageStatus.Complete
        })
    ]);

    private async Task<string> RenderAsync(ConditionActivations? activations = null, params Ability[] abilities)
    {
        var unit = new Unit(
            new Datasheet("Chaos Lord", keywords: [], abilities: abilities,
                statlines: [("Chaos Lord", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [Hammer]),
            [], [new ModelLine("Chaos Lord", [Hammer.Name], count: 1)])
        {
            ConditionActivations = activations ?? ConditionActivations.Empty
        };
        var view = AttachedUnitAggregator.Build(unit, Catalogue);

        using var scope = factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();
        var model = new UnitBlockRenderModel(0, LivePlayModel.BuildUnitBlock(view, unit, Catalogue),
            RuleGlossary.Build(new BsdataClosure([])));
        return await renderer.RenderAsync(httpContext, "/Pages/Shared/_UnitBlock.cshtml", model);
    }

    // The panel a trigger opens, up to the next panel; the pattern's group 1 is the panel id.
    private static string PanelOf(string html, string triggerPattern)
    {
        var target = Regex.Match(html, triggerPattern).Groups[1].Value;
        target.Should().NotBeEmpty();
        return PanelById(html, target);
    }

    private static string PanelById(string html, string id)
    {
        var start = html.IndexOf($"<div id=\"{id}\"", StringComparison.Ordinal);
        var next = html.IndexOf("<div id=\"p-", start + 1, StringComparison.Ordinal);
        return next < 0 ? html[start..] : html[start..next];
    }

    private static string AbilityPanel(string html, string name) =>
        PanelOf(html, $"class=\"ability-name-line\" popovertarget=\"(p-[^\"]+)\">{Regex.Escape(name)}</button>");

    [Fact]
    public async Task ChanceForGlory_ShowsOneSwitchLabelledWithItsUsageLimit_OffByDefault()
    {
        var html = await RenderAsync(null, Glory);

        var panel = AbilityPanel(html, "Chance for Glory");
        panel.Should().Contain("class=\"apply-section\"");
        var inputs = Regex.Matches(panel, "<input [^>]*>").Select(m => m.Value).ToList();
        inputs.Should().ContainSingle();
        inputs[0].Should().Contain("type=\"checkbox\"").And.Contain("role=\"switch\"")
            .And.Contain("data-unit-index=\"0\"").And.Contain("data-ability=\"Chance for Glory\"")
            .And.Contain("data-condition=\"\"").And.NotContain("checked");
        panel.Should().Contain("<span class=\"apply-label\">Once per battle</span>");
        panel.IndexOf("Chance for Glory rules text.", StringComparison.Ordinal)
            .Should().BeLessThan(panel.IndexOf("apply-section", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnActiveSwitch_RendersChecked()
    {
        var html = await RenderAsync(ActivationFixtures.Condition("Chance for Glory"), Glory);

        AbilityPanel(html, "Chance for Glory").Should().MatchRegex("role=\"switch\"[^>]* checked>");
    }

    [Fact]
    public async Task DarkPacts_ShowsNoneThenEachOptionAsRadios_WithNoneChecked()
    {
        var html = await RenderAsync(null, DarkPacts);

        var panel = AbilityPanel(html, "Dark Pacts");
        panel.Should().Contain("data-max=\"1\"");
        Regex.Matches(panel, "<span class=\"apply-label\">([^<]+)</span>").Select(m => m.Groups[1].Value)
            .Should().Equal("None", "[LETHAL HITS]", "[SUSTAINED HITS 1]");
        var radios = Regex.Matches(panel, "<input [^>]*>").Select(m => m.Value).ToList();
        radios.Should().HaveCount(3).And
            .OnlyContain(r => r.Contains("type=\"radio\"") && r.Contains("data-group=\"0\""));
        radios[0].Should().Contain("data-option=\"-1\"").And.Contain("checked");
        radios.Skip(1).Should().OnlyContain(r => !r.Contains("checked"));
    }

    [Fact]
    public async Task ASelectedPact_RendersItsOptionChecked()
    {
        var html = await RenderAsync(ActivationFixtures.Choice("Dark Pacts", 0, 0), DarkPacts);

        var radios = Regex.Matches(AbilityPanel(html, "Dark Pacts"), "<input [^>]*>").Select(m => m.Value).ToList();
        radios[1].Should().Contain("data-option=\"0\"").And.Contain("checked");
        radios[0].Should().NotContain("checked");
    }

    [Fact]
    public async Task AMultiSelectGroup_RendersCheckboxesCarryingItsMaximum()
    {
        var html = await RenderAsync(null, Frenzy);

        var panel = AbilityPanel(html, "Frenzy");
        panel.Should().Contain("data-max=\"2\"").And.NotContain(">None<");
        Regex.Matches(panel, "<input [^>]*>").Select(m => m.Value).Should().HaveCount(3)
            .And.OnlyContain(i => i.Contains("type=\"checkbox\"") && !i.Contains("role=\"switch\""));
    }

    [Fact]
    public async Task TheAbilityPopoverNestedInANotAddedChip_AlsoHasControls_WithItsOwnRadioName()
    {
        var html = await RenderAsync(null, DarkPacts);

        var chipPanel = PanelOf(html, "class=\"weapon-tag weapon-tag-cond\" popovertarget=\"(p-[^\"]+)\"");
        var nested = PanelById(html,
            Regex.Match(chipPanel, "class=\"ability-name-line\" popovertarget=\"(p-[^\"]+)\"").Groups[1].Value);
        nested.Should().Contain("data-depth=\"1\"").And.Contain("class=\"apply-section\"");

        var names = Regex.Matches(html, "type=\"radio\" name=\"([^\"]+)\"").Select(m => m.Groups[1].Value)
            .Distinct().ToList();
        names.Count.Should().BeGreaterThan(1);
        Regex.Matches(html, "type=\"radio\" name=\"([^\"]+)\"").GroupBy(m => m.Groups[1].Value)
            .Should().OnlyContain(g => g.Count() == 3);
    }

    [Fact]
    public async Task AnActivatedStrengthChange_IsAmber_AndItsLineNotesItWasActivated()
    {
        var html = await RenderAsync(ActivationFixtures.Condition("Chance for Glory"), Glory);

        var panel = PanelOf(html, "class=\"provenance-tile\" popovertarget=\"(p-[^\"]+)\">9<");
        panel.Should().MatchRegex("<span data-prov-original>8</span>")
            .And.MatchRegex(">Chance for Glory</button></td><td>\\+1</td>")
            .And.Contain("<tr class=\"provenance-note\"><td colspan=\"2\">activated</td></tr>")
            .And.MatchRegex("<span data-prov-total>9</span>")
            .And.NotContain("provenance-not-applied");
    }

    [Fact]
    public async Task ASelectedPactsChip_IsGranted_AndNotesItWasActivated()
    {
        var html = await RenderAsync(ActivationFixtures.Choice("Dark Pacts", 0, 0), DarkPacts);

        html.Should().NotContain("weapon-tag-cond");
        PanelOf(html, "class=\"weapon-tag weapon-tag-granted\" popovertarget=\"(p-[^\"]+)\">Lethal Hits<")
            .Should().Contain("<td colspan=\"2\">activated</td>");
    }

    [Fact]
    public async Task AnAbilityWithNoActivatableCondition_HasNoApplySection()
    {
        var unconditional = new Ability
        {
            Name = "Plain", Text = "Plain rules text.", Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };

        var html = await RenderAsync(null, unconditional);

        html.Should().NotContain("apply-section");
    }

    [Fact]
    public async Task TheArmyHeadersRulePopover_HasNoControls()
    {
        var pacts = DarkPacts with { Origin = AbilityOrigin.ArmyRule };
        var unit = new Unit(
            new Datasheet("Chaos Lord", keywords: [], abilities: [pacts],
                statlines: [("Chaos Lord", new Statline(6, 4, 3, 5, 6, 1))], weaponProfiles: [Hammer]),
            [], [new ModelLine("Chaos Lord", [Hammer.Name], count: 1)]);
        var roster = new ArmyRoster("Test", 0, ["Chaos"], [new ResolvedDetachment("Test Detachment", [])], "Test",
            "Incursion", 1000, [unit]);

        using var scope = factory.Services.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var renderer = scope.ServiceProvider.GetRequiredService<IRazorPartialRenderer>();
        var html = await renderer.RenderAsync(httpContext, "/Pages/Shared/_ArmyHeader.cshtml",
            new ArmyHeaderRenderModel(LivePlayModel.BuildArmyHeader(roster),
                RuleGlossary.Build(new BsdataClosure([]))));

        html.Should().Contain("Dark Pacts rules text.").And.NotContain("apply-section");
    }
}