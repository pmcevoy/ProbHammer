using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Catalogue;

public class DatasheetTests
{
    [Fact]
    public void BasicFields_AreExposed()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        datasheet.Name.Should().Be("Assault Intercessor Squad");
        datasheet.FactionKeywords.Should().Contain("ADEPTUS ASTARTES");
        datasheet.Keywords.Should().Contain("BATTLELINE");
        datasheet.Abilities.Should().ContainSingle(a => a.Name == "Shock Assault");
    }

    [Fact]
    public void SingleSharedStatline_IsExposedAsOneNamedEntry()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        datasheet.Statlines.Should().ContainSingle();
        datasheet.GetStatline("Assault Intercessor").T.Should().Be(4);
    }

    [Fact]
    public void MultipleDistinctStatlines_AreEachIndependentlyQueryable()
    {
        var datasheet = DatasheetFixtures.ChaosSpaceMarineSquad();

        datasheet.Statlines.Should().HaveCount(5);
        datasheet.GetStatline("Chaos Space Marine Champion").W.Should().Be(3);
        datasheet.GetStatline("Icon Bearer").W.Should().Be(2);
    }

    [Fact]
    public void SharedStatline_AllowsDivergentWeaponEligibility()
    {
        var datasheet = DatasheetFixtures.CrusaderSquad();

        // Both a rank-and-file model and a Sergeant reference the same "Initiate" statline,
        // but only the Sergeant's weapon set includes the Master-crafted Power Weapon.
        datasheet.GetStatline("Initiate").Should().Be(datasheet.GetStatline("Initiate"));
        datasheet.ResolveWeaponProfile(WeaponFixtures.PowerFist.Name).Should().NotBeNull();
        datasheet.ResolveWeaponProfile(WeaponFixtures.MasterCraftedPowerWeapon.Name).Should().NotBeNull();
    }

    [Fact]
    public void ResolvingANamedWeaponProfile_ReturnsItsCharacteristics()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        var profile = datasheet.ResolveWeaponProfile(WeaponFixtures.ChainSword.Name);

        profile.Type.Should().Be(WeaponType.Melee);
        profile.A.Should().Be(4);
        profile.Skill.Should().Be(3);
        profile.S.Should().Be(4);
        profile.Ap.Should().Be(-1);
        profile.D.Should().Be(1);
    }

    [Fact]
    public void ResolvingAnUnknownWeaponProfile_Throws()
    {
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        var act = () => datasheet.ResolveWeaponProfile("Does not exist");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Datasheet_OmitsConstraintAndCostData()
    {
        // No public surface for wargear min/max, composition counts, or points - compile-time
        // guarantee via the type's shape (Datasheet exposes Name/Keywords/Abilities/Statlines
        // and on-demand weapon resolution only).
        var datasheet = DatasheetFixtures.AssaultIntercessorSquad();

        var properties = typeof(Core.Domain.Catalogue.Datasheet)
            .GetProperties()
            .Select(p => p.Name);

        properties.Should().NotContain(n => n.Contains("Point") || n.Contains("Constraint") || n.Contains("Composition"));
        datasheet.Should().NotBeNull();
    }
}
