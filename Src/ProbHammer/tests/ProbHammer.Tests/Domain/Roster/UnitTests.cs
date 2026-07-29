using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;
using ProbHammer.Tests.Domain.Fixtures;

namespace ProbHammer.Tests.Domain.Roster;

public class UnitTests
{
    [Fact]
    public void UniformLoadout_ProducesOneModelLine()
    {
        var unit = UnitFixtures.SwordBrethrenSquadUniform();

        unit.ModelLines.Should().ContainSingle();
        unit.ModelLines[0].Count.Should().Be(4);
    }

    [Fact]
    public void MixedLoadoutWithinOneStatline_ProducesSeparateModelLinesSharingTheStatline()
    {
        var unit = UnitFixtures.CrusaderSquadMixedLoadout();

        unit.ModelLines.Should().HaveCount(2);
        unit.ModelLines.Should().OnlyContain(ml => ml.StatlineName == "Initiate");

        var powerFistLine = unit.ModelLines.Single(ml => ml.Weapons.Contains(WeaponFixtures.PowerFist().Name));
        var powerWeaponLine = unit.ModelLines.Single(ml => ml.Weapons.Contains(WeaponFixtures.MasterCraftedPowerWeapon().Name));

        powerFistLine.Count.Should().Be(2);
        powerWeaponLine.Count.Should().Be(3);
    }

    [Fact]
    public void StatlineReference_IsExplicitAndNotInferredFromWeapons()
    {
        // Constructing a ModelLine takes the statline name directly - there is no code path that
        // derives it from the weapon selection.
        var modelLine = new ModelLine("Initiate", [WeaponFixtures.PowerFist().Name], count: 2);

        modelLine.StatlineName.Should().Be("Initiate");
    }
}
