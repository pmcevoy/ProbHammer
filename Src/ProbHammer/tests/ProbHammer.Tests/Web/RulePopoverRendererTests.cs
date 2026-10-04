using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Web.Pages;
using ProbHammer.Web.Rendering;

namespace ProbHammer.Tests.Web;

public class RulePopoverRendererTests
{
    [Fact]
    public void BuildProvenancePopover_RendersItsTitleAndTable_WithTheAbilityPanelAsASiblingAfterIt()
    {
        var crusadeOfWrath = new Ability
        {
            Name = "Crusade of Wrath", Text = "Add 1 to the Strength characteristic.",
            Scope = AbilityScope.Unit, Origin = AbilityOrigin.Intrinsic
        };
        var provenance = new ValueProvenance("S · Power fist", "Original", "8",
            [new ProvenanceLine(crusadeOfWrath, "Crusade of Wrath", "+1", [], Applied: true)], "Total", "9");
        var renderer = new RulePopoverRenderer(RuleGlossary.Build(new BsdataClosure([])), "u0");

        var (trigger, trailer) = renderer.BuildProvenancePopover("9", "provenance-tile", provenance);

        trigger.Should().Be(
            "<button type=\"button\" id=\"t-u0-0\" class=\"provenance-tile\" popovertarget=\"p-u0-0\">9</button>");
        trailer.Should().StartWith("<div id=\"p-u0-0\" class=\"rule-popover provenance-popover\" popover=\"auto\"")
            .And.Contain("<div class=\"rule-popover-title\">S &#183; Power fist")
            .And.Contain("<span data-prov-original>8</span>")
            .And.Contain("<span data-prov-total>9</span>");
        var provenancePanelEnd = trailer.IndexOf("</table></div></div>", StringComparison.Ordinal);
        trailer.IndexOf("popovertarget=\"p-u0-1\">Crusade of Wrath</button>", StringComparison.Ordinal)
            .Should().BeInRange(0, provenancePanelEnd);
        trailer.IndexOf("<div id=\"p-u0-1\" class=\"rule-popover\" popover=\"auto\" data-depth=\"1\">",
            StringComparison.Ordinal).Should().BeGreaterThan(provenancePanelEnd);
    }
}
