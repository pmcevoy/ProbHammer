using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataFactionResolverTests
{
    [Fact]
    public void SubFaction_ResolvesViaSuffixMatch_NotALiteralJoin()
    {
        var files = new[] { "Imperium - Black Templars.json", "Imperium - Space Marines.json", "Orks.json" };

        var result = BsdataFactionResolver.ResolveStartingFileName(["Space Marines", "Black Templars"], files);

        result.Should().Be("Imperium - Black Templars.json");
    }

    [Fact]
    public void UnsplitFaction_ResolvesViaDirectFilenameMatch()
    {
        var files = new[] { "Orks.json", "Necrons.json" };

        var result = BsdataFactionResolver.ResolveStartingFileName(["Orks"], files);

        result.Should().Be("Orks.json");
    }

    [Fact]
    public void LibraryFile_IsNeverSelected_EvenWhenItWouldOtherwiseMatch()
    {
        var files = new[] { "Chaos - Chaos Daemons Library.json" };

        var act = () => BsdataFactionResolver.ResolveStartingFileName(["Chaos Daemons"], files);

        act.Should().Throw<BsdataFactionResolutionException>();
    }

    [Fact]
    public void NoMatchingFile_FailsLoudNamingTheFactionEntry()
    {
        var files = new[] { "Orks.json" };

        var act = () => BsdataFactionResolver.ResolveStartingFileName(["Necrons"], files);

        act.Should().Throw<BsdataFactionResolutionException>().Which.Faction.Should().Be("Necrons");
    }

    [Fact]
    public void MoreThanOneMatchingFile_FailsLoudNamingTheFactionEntry()
    {
        var files = new[] { "Imperium - Black Templars.json", "Chaos - Black Templars.json" };

        var act = () => BsdataFactionResolver.ResolveStartingFileName(["Black Templars"], files);

        act.Should().Throw<BsdataFactionResolutionException>().Which.Faction.Should().Be("Black Templars");
    }
}
