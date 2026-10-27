using FluentAssertions;

namespace ProbHammer.Tools.AbilityPipeline.Classifier.Tests;

public class IncrementalSelectorTests
{
    [Fact]
    public void AStaleHash_IsNotSelected()
    {
        var corpus = new Dictionary<string, ExtractionRecord>
        {
            ["live"] = Fixtures.Extraction("live", "Live text."),
            ["stale"] = Fixtures.Extraction("stale", "Stale text.", Fixtures.Today.AddDays(-23))
        };

        var pending = IncrementalSelector.SelectPending(corpus, new Dictionary<string, ClassificationRecord>(), "v3");

        pending.Select(r => r.Hash).Should().Equal("live");
    }
}
