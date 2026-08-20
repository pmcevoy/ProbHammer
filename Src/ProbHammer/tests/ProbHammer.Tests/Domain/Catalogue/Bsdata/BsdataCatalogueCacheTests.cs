using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class BsdataCatalogueCacheTests
{
    [Fact]
    public void SecondRequestForTheSameStartingFile_ReusesTheCachedClosure_WithoutRereadingSource()
    {
        var source = new CountingSource();
        var cache = new BsdataCatalogueCache(source);

        var first = cache.GetOrBuild("a.json");
        var callsAfterFirst = source.GetJsonCalls;
        var second = cache.GetOrBuild("a.json");

        second.Should().BeSameAs(first);
        source.GetJsonCalls.Should().Be(callsAfterFirst);
    }

    [Fact]
    public void TwoDifferentStartingFiles_AreCachedIndependently()
    {
        var source = new CountingSource();
        var cache = new BsdataCatalogueCache(source);

        var a = cache.GetOrBuild("a.json");
        var b = cache.GetOrBuild("b.json");
        var aAgain = cache.GetOrBuild("a.json");

        a.Should().NotBeSameAs(b);
        aAgain.Should().BeSameAs(a);
    }

    private sealed class CountingSource : IBsdataCatalogueSource
    {
        public int GetJsonCalls { get; private set; }

        public string GetJson(string fileName)
        {
            GetJsonCalls++;
            return $"{{\"catalogue\":{{\"id\":\"{fileName}\",\"name\":\"{fileName}\"}}}}";
        }

        public IReadOnlyList<string> ListFileNames() => ["a.json", "b.json"];
    }
}
