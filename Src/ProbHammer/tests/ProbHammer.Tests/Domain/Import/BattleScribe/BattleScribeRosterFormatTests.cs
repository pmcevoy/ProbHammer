using FluentAssertions;
using ProbHammer.Core.Domain.Import.BattleScribe;

namespace ProbHammer.Tests.Domain.Import.BattleScribe;

/// <summary>Covers battlescribe-roster-import's Format Recognition requirement: non-JSON text and
/// JSON without the expected <c>roster.xmlns</c> shape both fall through unrecognized (and, per
/// army-list-import's Import Submission requirement, on to the existing GW-app text pipeline)
/// rather than being misidentified - see ImportFlowTests for the end-to-end version of this same
/// scenario through the real `/Import` page.</summary>
public class BattleScribeRosterFormatTests
{
    [Fact]
    public void ValidBattleScribeRosterJson_IsRecognized()
    {
        const string json = """{"roster": {"xmlns": "http://www.battlescribe.net/schema/rosterSchema", "name": "Test", "costs": [], "costLimits": [], "forces": []}}""";

        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeTrue();
        roster.Should().NotBeNull();
        roster!.Name.Should().Be("Test");
    }

    [Fact]
    public void PlainGwAppExportText_IsNotRecognized()
    {
        const string text = "For the Emperor (1065 Points)\n\nSpace Marines\n";

        BattleScribeRosterFormat.TryParse(text, out var roster).Should().BeFalse();
        roster.Should().BeNull();
    }

    [Fact]
    public void NonRosterJson_IsNotRecognized()
    {
        const string json = """{"someOtherJson": true}""";

        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeFalse();
        roster.Should().BeNull();
    }

    [Fact]
    public void JsonWithRosterButWrongXmlns_IsNotRecognized()
    {
        const string json = """{"roster": {"xmlns": "http://example.com/some-other-schema"}}""";

        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeFalse();
        roster.Should().BeNull();
    }

    [Fact]
    public void MalformedJson_IsNotRecognized()
    {
        const string json = "{not valid json at all";

        BattleScribeRosterFormat.TryParse(json, out var roster).Should().BeFalse();
        roster.Should().BeNull();
    }
}
