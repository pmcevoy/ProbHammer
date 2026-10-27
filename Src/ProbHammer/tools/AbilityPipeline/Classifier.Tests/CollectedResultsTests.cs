using FluentAssertions;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Tests;

public class CollectedResultsTests
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    [Fact]
    public void CollectingToANamedFile_LeavesTheClassificationFileByteIdentical()
    {
        var classificationsPath = Path.Combine(_dir, "classifications.json");
        ClassificationFile.Save(classificationsPath,
            new Dictionary<string, ClassificationRecord> { ["old"] = Fixtures.Record("old", Fixtures.Classification()) });
        var before = File.ReadAllBytes(classificationsPath);
        var outPath = Path.Combine(_dir, "v3-draft-sonnet-5.json");
        var collected = new Dictionary<string, ClassificationRecord>
        {
            ["old"] = Fixtures.Record("old", Fixtures.Classification(new NamedAbilityGrantEffect("Stealth"))) with
            {
                Usage = new TokenUsage(1200, 340)
            }
        };

        var written = CollectedResults.Save(classificationsPath, outPath, collected);

        written.Should().Be(outPath);
        File.ReadAllBytes(classificationsPath).Should().Equal(before);
        ClassificationFile.Load(outPath)["old"].Usage.Should().Be(new TokenUsage(1200, 340));
    }

    [Fact]
    public void CollectingWithoutANamedFile_MergesIntoTheClassificationFile()
    {
        var classificationsPath = Path.Combine(_dir, "classifications.json");
        ClassificationFile.Save(classificationsPath,
            new Dictionary<string, ClassificationRecord> { ["kept"] = Fixtures.Record("kept", Fixtures.Classification()) });

        CollectedResults.Save(classificationsPath, null,
            new Dictionary<string, ClassificationRecord> { ["new"] = Fixtures.Record("new", Fixtures.Classification()) });

        ClassificationFile.Load(classificationsPath).Keys.Should().BeEquivalentTo("kept", "new");
    }
}
