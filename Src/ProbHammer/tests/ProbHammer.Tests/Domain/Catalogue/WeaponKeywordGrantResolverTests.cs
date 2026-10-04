using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class WeaponKeywordGrantResolverTests
{
    [Fact]
    public void AKeywordTheWeaponLacks_IsAdded()
    {
        var result = WeaponKeywordGrantResolver.Apply(["Assault", "Pistol"], "Lethal Hits");

        result.Keywords.Should().Equal("Assault", "Pistol", "Lethal Hits");
        result.Added.Should().Be("Lethal Hits");
        result.Replaced.Should().BeNull();
    }

    [Fact]
    public void AKeywordTheWeaponAlreadyHas_ChangesNothing()
    {
        var result = WeaponKeywordGrantResolver.Apply(["LETHAL HITS"], "[Lethal Hits]");

        result.Keywords.Should().Equal("LETHAL HITS");
        result.Changed.Should().BeFalse();
    }

    [Fact]
    public void ABetterValue_ReplacesTheWeaponsOwnKeyword()
    {
        var result = WeaponKeywordGrantResolver.Apply(["Assault", "Sustained Hits 1"], "Sustained Hits 2");

        result.Keywords.Should().Equal("Assault", "Sustained Hits 2");
        result.Added.Should().Be("Sustained Hits 2");
        result.Replaced.Should().Be("Sustained Hits 1");
    }

    [Theory]
    [InlineData("Sustained Hits 2", "Sustained Hits 1")]
    [InlineData("Sustained Hits 2", "Sustained Hits 2")]
    [InlineData("Anti-Infantry 2+", "Anti-Infantry 4+")]
    public void AWorseOrEqualValue_ChangesNothing(string own, string granted)
    {
        var result = WeaponKeywordGrantResolver.Apply([own], granted);

        result.Keywords.Should().Equal(own);
        result.Changed.Should().BeFalse();
    }
}
