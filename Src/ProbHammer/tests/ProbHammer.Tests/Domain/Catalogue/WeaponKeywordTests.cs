using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Catalogue;

public class WeaponKeywordTests
{
    [Fact]
    public void TwoValuesOfTheSameKeyword_ShareOneIdentity()
    {
        var one = WeaponKeyword.Parse("Sustained Hits 1");
        var two = WeaponKeyword.Parse("SUSTAINED HITS 2");

        one.Identity.Should().Be(two.Identity);
        one.Value.Should().Be("1");
        two.Value.Should().Be("2");
    }

    [Fact]
    public void AntiKeywordsWithDifferentTargets_AreDifferentKeywords()
    {
        WeaponKeyword.Parse("Anti-Infantry 4+").Identity
            .Should().NotBe(WeaponKeyword.Parse("Anti-Vehicle 4+").Identity);
    }

    [Fact]
    public void BracketsAndCasing_DoNotChangeIdentity()
    {
        var bracketed = WeaponKeyword.Parse("[ANTI-TITANIC 4+]");
        var plain = WeaponKeyword.Parse("Anti-Titanic 4+");

        bracketed.Identity.Should().Be(plain.Identity);
        bracketed.Value.Should().Be(plain.Value).And.Be("4+");
        bracketed.Display.Should().Be("ANTI-TITANIC 4+");
    }

    [Fact]
    public void AKeywordWithoutAValue_HasNoValue()
    {
        var a = WeaponKeyword.Parse("Lethal Hits");
        var b = WeaponKeyword.Parse("LETHAL HITS");

        a.Identity.Should().Be(b.Identity);
        a.Value.Should().BeNull();
        b.Value.Should().BeNull();
    }

    [Fact]
    public void AQualifier_StaysPartOfTheIdentity()
    {
        var qualified = WeaponKeyword.Parse("[SUSTAINED HITS 1: non-MONSTER/VEHICLE]");

        qualified.Identity.Should().NotBe(WeaponKeyword.Parse("Sustained Hits 1").Identity);
        qualified.Value.Should().Be("1");
    }

    [Fact]
    public void AHigherSustainedHitsValue_IsBetter()
    {
        WeaponKeyword.Compare(WeaponKeyword.Parse("Sustained Hits 2"), WeaponKeyword.Parse("Sustained Hits 1"))
            .Should().BePositive();
    }

    [Fact]
    public void ALowerAntiThreshold_IsBetter()
    {
        WeaponKeyword.Compare(WeaponKeyword.Parse("Anti-Infantry 2+"), WeaponKeyword.Parse("Anti-Infantry 4+"))
            .Should().BePositive();
    }

    [Fact]
    public void ADiceValue_ComparesByItsExpectedValue()
    {
        WeaponKeyword.Compare(WeaponKeyword.Parse("Sustained Hits D3"), WeaponKeyword.Parse("Sustained Hits 1"))
            .Should().BePositive();
    }

    [Fact]
    public void EveryCatalogueGrantKeyword_ParsesToASaneIdentity()
    {
        var keywords = ClassificationFixtures.CheckedIn.Records
            .SelectMany(r => r.Classification.Effects)
            .Select(e => e.Effect)
            .OfType<WeaponKeywordGrantEffect>()
            .Select(g => g.Keyword)
            .Distinct()
            .ToList();

        keywords.Should().NotBeEmpty();
        foreach (var keyword in keywords)
        {
            var parsed = WeaponKeyword.Parse(keyword);
            parsed.Identity.Should().MatchRegex("^[a-z0-9]+(:[a-z0-9]+)?$", because: keyword);
            parsed.Display.Should().NotStartWith("[").And.NotEndWith("]");
            if (char.IsDigit(keyword.TrimEnd(']', '+')[^1]))
                parsed.Value.Should().NotBeNull(because: keyword);
        }
    }
}
