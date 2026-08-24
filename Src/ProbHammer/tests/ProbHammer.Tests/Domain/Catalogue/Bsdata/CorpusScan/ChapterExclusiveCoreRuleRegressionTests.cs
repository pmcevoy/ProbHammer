using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>Not a full-corpus scan - a small, targeted, [Fact(Explicit = true)] regression check
/// (needs the live clone) resolving the same real "Scout Squad" case gate-and-dedupe-core-rule-
/// abilities was found and fixed against by hand this session: before the fix, resolving against
/// both a Black Templars and a Salamanders starting closure produced the identical ability list
/// (both "Oath of Moment" and "Templar Vows" together) - factually wrong for both, since the two
/// rules are chapter-exclusive replacements for each other. Keeps that specific real-world case
/// from silently regressing.</summary>
public class ChapterExclusiveCoreRuleRegressionTests
{
    [Fact(Explicit = true)]
    public void BlackTemplars_GetsTemplarVowsOnly_NotOathOfMoment()
    {
        var source = LiveClone.RequireSource();
        var catalogue = ResolvedBsdataCatalogue.Build(source, "Imperium - Black Templars.json");

        var scoutSquad = catalogue.ResolveDatasheet("Scout Squad");

        scoutSquad.Abilities.Select(a => a.Name).Should().Contain("Templar Vows");
        scoutSquad.Abilities.Select(a => a.Name).Should().NotContain("Oath of Moment");
    }

    [Fact(Explicit = true)]
    public void Salamanders_GetsOathOfMomentOnly_NotTemplarVows()
    {
        var source = LiveClone.RequireSource();
        var catalogue = ResolvedBsdataCatalogue.Build(source, "Imperium - Salamanders.json");

        var scoutSquad = catalogue.ResolveDatasheet("Scout Squad");

        scoutSquad.Abilities.Select(a => a.Name).Should().Contain("Oath of Moment");
        scoutSquad.Abilities.Select(a => a.Name).Should().NotContain("Templar Vows");
    }
}
