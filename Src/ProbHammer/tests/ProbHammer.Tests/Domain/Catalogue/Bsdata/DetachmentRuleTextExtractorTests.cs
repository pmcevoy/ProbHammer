using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Covers catalogue-json-ingestion's Detachment Rule Text Extraction requirement against
/// tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/detachment-resolution.json - a hand-built fixture
/// carrying both real BSData rule-text shapes (a locally-declared `rules[]` entry, a `"type":
/// "rule"` infoLink) side by side with several zero-rule Detachments.</summary>
public class DetachmentRuleTextExtractorTests
{
    private static ResolvedBsdataCatalogue Catalogue() =>
        ResolvedBsdataCatalogue.Build(BsdataFixtures.Source(), "detachment-resolution.json");

    private static BsSelectionEntry Entry(string name) => Catalogue().ResolveDetachment(name);

    [Fact]
    public void ALocallyDeclaredRule_ExtractsDirectly()
    {
        var catalogue = Catalogue();
        var pairs = DetachmentRuleTextExtractor.Extract(Entry("Marshal's Household"), catalogue.Glossary);

        pairs.Should().ContainSingle(p => p.Name == "Faith-Fuelled Resolve" && p.Text == "Full text of Faith-Fuelled Resolve.");
    }

    [Fact]
    public void AnInfoLinkReferencedRule_ResolvesViaTheGlossary()
    {
        var catalogue = Catalogue();
        var pairs = DetachmentRuleTextExtractor.Extract(Entry("Awakened Dynasty"), catalogue.Glossary);

        pairs.Should().Contain(p => p.Name == "Command Protocols" && p.Text == "Full text of Command Protocols.");
    }

    [Fact]
    public void ADetachmentWithMoreThanOneRule_ExtractsAllOfThem()
    {
        var catalogue = Catalogue();
        var pairs = DetachmentRuleTextExtractor.Extract(Entry("Black Spear Task Force"), catalogue.Glossary);

        pairs.Should().HaveCount(2);
        pairs.Should().Contain(p => p.Name == "Kill Teams");
        pairs.Should().Contain(p => p.Name == "Mission Tactics");
    }

    [Fact]
    public void AnUnresolvableInfoLinkReference_IsSkippedNotFailed()
    {
        var catalogue = Catalogue();
        var pairs = DetachmentRuleTextExtractor.Extract(Entry("Awakened Dynasty"), catalogue.Glossary);

        pairs.Should().NotContain(p => p.Name == "Nonexistent Rule");
        pairs.Should().ContainSingle();
    }

    [Fact]
    public void ADetachmentWithNoRuleOfItsOwn_ProducesNoPairs()
    {
        var catalogue = Catalogue();
        var pairs = DetachmentRuleTextExtractor.Extract(Entry("Warhost"), catalogue.Glossary);

        pairs.Should().BeEmpty();
    }
}
